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
        // hepsi tek bir transaction'da commit edilir.
        // Pessimistic: instance satırı UPDLOCK, ROWLOCK ile okunur; aynı instance'a gelen ikinci event, ilki commit edene kadar
        // okumada bekler ve güncel durumu görür. (Optimistic modda iki okuma da RepeatableRead S kilidi tutup UPDATE'te
        // birbirini beklediği için SQL deadlock'u oluşuyordu.) Concurrency kontrolü bu kilitle sağlanır; RowVersion yedek savunma olarak kalır.
        x.AddSagaStateMachine<OrderStateMachine, OrderState>()
            .EntityFrameworkRepository(r =>
            {
                r.ConcurrencyMode = ConcurrencyMode.Pessimistic;
                r.ExistingDbContext<OrderDbContext>();
                // SqlServerLockStatementProvider: SELECT ... WITH (UPDLOCK, ROWLOCK)
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
