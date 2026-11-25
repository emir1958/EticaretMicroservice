using EticaretMicroservice.Payment.Api.Consumers;
using EticaretMicroservice.Payment.Api.Data;
using EticaretMicroservice.Payment.Api.Services;
using EticaretMicroservice.Shared.Events;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EticaretMicroservice.Payment.IntegrationTests.Infrastructure;

public sealed class PaymentTestHost : IAsyncDisposable
{
    private static readonly TimeSpan SettleDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(20);

    private readonly ServiceProvider _provider;

    private PaymentTestHost(ServiceProvider provider, ITestHarness harness)
    {
        _provider = provider;
        Harness = harness;
    }

    public ITestHarness Harness { get; }

    public static async Task<PaymentTestHost> StartAsync(string connectionString, IPaymentService paymentService)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<PaymentDbContext>(o => o.UseSqlServer(connectionString));
        services.AddSingleton(paymentService);
        services.AddMassTransitTestHarness(x =>
        {
            x.AddConsumer<RefundPaymentCommandConsumer>();
            x.AddConsumer<PaymentRefundedSink>();
            x.AddEntityFrameworkOutbox<PaymentDbContext>(o =>
            {
                o.UseSqlServer();
                o.UseBusOutbox();
            });

            x.UsingInMemory((context, cfg) =>
            {
                // Program.cs'teki payment-refund-queue tanımının karşılığı. Outbox burada da olmalı: consumer'ın
                // UPDLOCK'u ancak endpoint'in outbox transaction'ı boyunca tutulur. Program.cs değişirse burası da güncellenmeli.
                cfg.ReceiveEndpoint("payment-refund-queue", e =>
                {
                    e.UseEntityFrameworkOutbox<PaymentDbContext>(context);
                    e.ConfigureConsumer<RefundPaymentCommandConsumer>(context);
                });

                cfg.ReceiveEndpoint("payment-refunded-sink", e => e.ConfigureConsumer<PaymentRefundedSink>(context));
            });
        });

        var provider = services.BuildServiceProvider(validateScopes: true);
        var harness = provider.GetRequiredService<ITestHarness>();
        harness.TestTimeout = WaitTimeout;
        await harness.Start();

        return new PaymentTestHost(provider, harness);
    }

    public Task PublishAsync<T>(T message) where T : class =>
        Harness.Bus.Publish(message, ctx => ctx.MessageId = NewId.NextGuid());

    // Outbox mesajları commit sonrası gönderildiği için harness'ın Published listesi bunları tanımaz;
    // Order'a ulaşacak event, onun yerini tutan sink'in tükettikleri üzerinden sayılır.
    public int RefundedEventCount(int orderId) =>
        Harness.Consumed.Select<PaymentRefundedEvent>().Count(m => m.Context.Message.OrderId == orderId);

    public static async Task WaitUntilAsync(Func<Task<bool>> condition, string description)
    {
        var deadline = DateTime.UtcNow + WaitTimeout;
        while (DateTime.UtcNow < deadline)
        {
            if (await condition())
                return;

            await Task.Delay(100);
        }

        throw new TimeoutException($"Beklenen durum {WaitTimeout.TotalSeconds} sn içinde oluşmadı: {description}");
    }

    public static Task SettleAsync() => Task.Delay(SettleDelay);

    public async ValueTask DisposeAsync()
    {
        await Harness.Stop();
        await _provider.DisposeAsync();
    }
}
