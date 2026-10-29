using EticaretMicroservice.Stock.Api.Data;
using EticaretMicroservice.Stock.Api.Extensions;
using EticaretMicroservice.Stock.Api.Services;
using MassTransit;
using MassTransit.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EticaretMicroservice.Stock.IntegrationTests.Infrastructure;

// Stock.Api'nin mesajlaşma tarafını production ile aynı kayıtlarla (AddStockMessaging) ayağa kaldırır.
// Tek fark: RabbitMQ yerine MassTransit'in in-memory test transport'u kullanılır.
public sealed class StockTestHost : IAsyncDisposable
{
    // "Hiçbir şey değişmedi" doğrulamalarından önce ikinci mesajın işlenmesine tanınan süre
    private static readonly TimeSpan SettleDelay = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan WaitTimeout = TimeSpan.FromSeconds(20);

    private readonly ServiceProvider _provider;

    private StockTestHost(ServiceProvider provider, ITestHarness harness)
    {
        _provider = provider;
        Harness = harness;
    }

    public ITestHarness Harness { get; }

    public static async Task<StockTestHost> StartAsync(string connectionString)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<StockDbContext>(o => o.UseSqlServer(connectionString));
        services.AddScoped<IStockService, StockService>();
        services.AddMassTransitTestHarness(x =>
        {
            x.AddStockMessaging();
            x.AddConsumer<DownstreamEventSink>();
        });

        var provider = services.BuildServiceProvider(validateScopes: true);
        var harness = provider.GetRequiredService<ITestHarness>();
        harness.TestTimeout = WaitTimeout;
        await harness.Start();

        return new StockTestHost(provider, harness);
    }

    public Task PublishAsync<T>(T message, Guid? messageId = null) where T : class =>
        Harness.Bus.Publish(message, ctx => ctx.MessageId = messageId ?? NewId.NextGuid());

    // Stock'un yayınladığı event'leri, aşağı akış servisinin (Payment/Order) gerçekten aldığı haliyle sayar.
    // Harness'ın Published listesi kullanılamaz: consumer outbox mesajları commit sonrası serileştirilmiş
    // haliyle yeniden gönderir ve tipli publish gözlemcisi bunları tanımaz.
    public int ReceivedDownstreamCount<T>(Func<T, bool> filter) where T : class =>
        Harness.Consumed.Select<T>().Count(m => filter(m.Context.Message));

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
