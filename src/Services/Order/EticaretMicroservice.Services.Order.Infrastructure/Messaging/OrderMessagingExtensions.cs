using EticaretMicroservice.Services.Order.Application.Sagas;
using EticaretMicroservice.Services.Order.Application.Sagas.Activities;
using EticaretMicroservice.Services.Order.Infrastructure.Persistence;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;

namespace EticaretMicroservice.Services.Order.Infrastructure.Messaging;

public static class OrderMessagingExtensions
{
    public const string SagaEndpointName = "order-saga";

    // Saga ve outbox kayıtları tek yerde: Program.cs (RabbitMQ) ve integration testler (in-memory) aynı kaydı kullanır.
    public static IBusRegistrationConfigurator AddOrderMessaging(this IBusRegistrationConfigurator x)
    {
        x.AddScoped<CompleteOrderActivity>();
        x.AddScoped<CancelOrderActivity>();

        // Saga durumu, Order aggregate'i ve outbox mesajları aynı OrderDbContext'i (ExistingDbContext) paylaşır:
        // hepsi tek bir transaction'da commit edilir. Aynı instance'a eşzamanlı yazımda kaybeden taraf retry ile yeniden
        // işlenir. Outbox transaction'ı RepeatableRead olduğu için çakışma pratikte deadlock olarak çıkar; RowVersion yedek savunmadır.
        x.AddSagaStateMachine<OrderStateMachine, OrderState>()
            .EntityFrameworkRepository(r =>
            {
                r.ConcurrencyMode = ConcurrencyMode.Optimistic;
                r.ExistingDbContext<OrderDbContext>();
                r.UseSqlServer();
            })
            .Endpoint(e => e.Name = SagaEndpointName);

        x.AddEntityFrameworkOutbox<OrderDbContext>(o =>
        {
            o.UseSqlServer();
            o.UseBusOutbox();
            o.DuplicateDetectionWindow = TimeSpan.FromMinutes(5);
        });

        // Consumer/saga outbox + inbox: saga'nın publish ettiği mesajlar DB değişiklikleriyle aynı commit'te kaydedilir
        x.AddConfigureEndpointsCallback((context, _, cfg) =>
        {
            cfg.UseEntityFrameworkOutbox<OrderDbContext>(context);
        });

        x.SetKebabCaseEndpointNameFormatter();

        return x;
    }
}
