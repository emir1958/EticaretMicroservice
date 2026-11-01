using EticaretMicroservice.Services.Order.Application.Sagas.Activities;
using EticaretMicroservice.Shared.Events;
using MassTransit;

namespace EticaretMicroservice.Services.Order.Application.Sagas;

// Sipariş sürecinin tek sahibi. Her durum için hangi event'in ne yapacağı burada açıkça tanımlıdır;
// tanımsız bir durum/event çifti MassTransit tarafından hata sayılır, bu yüzden "yoksay" kararları da yazılır.
public class OrderStateMachine : MassTransitStateMachine<OrderState>
{
    public State AwaitingStock { get; private set; } = null!;
    public State AwaitingPayment { get; private set; } = null!;
    public State Completed { get; private set; } = null!;
    public State Canceled { get; private set; } = null!;

    public Event<OrderCreatedEvent> OrderCreated { get; private set; } = null!;
    public Event<StockReservedEvent> StockReserved { get; private set; } = null!;
    public Event<StockFailedEvent> StockFailed { get; private set; } = null!;
    public Event<PaymentCompletedEvent> PaymentCompleted { get; private set; } = null!;
    public Event<PaymentFailedEvent> PaymentFailed { get; private set; } = null!;

    public OrderStateMachine()
    {
        InstanceState(x => x.CurrentState);

        Event(() => OrderCreated, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));

        // Stock/Payment cevabı, saga OrderCreated'ı işlemeden önce gelebilir (paralel kuyruklar).
        // Instance yoksa mesaj hata sayılır ve retry ile instance oluştuktan sonra yeniden işlenir.
        Event(() => StockReserved, x => { x.CorrelateById(ctx => ctx.Message.CorrelationId); x.OnMissingInstance(m => m.Fault()); });
        Event(() => StockFailed, x => { x.CorrelateById(ctx => ctx.Message.CorrelationId); x.OnMissingInstance(m => m.Fault()); });
        Event(() => PaymentCompleted, x => { x.CorrelateById(ctx => ctx.Message.CorrelationId); x.OnMissingInstance(m => m.Fault()); });
        Event(() => PaymentFailed, x => { x.CorrelateById(ctx => ctx.Message.CorrelationId); x.OnMissingInstance(m => m.Fault()); });

        Initially(
            When(OrderCreated)
                .Then(ctx =>
                {
                    ctx.Saga.OrderId = ctx.Message.OrderId;
                    ctx.Saga.BuyerId = ctx.Message.BuyerId;
                    ctx.Saga.CreatedAt = DateTime.UtcNow;
                })
                .TransitionTo(AwaitingStock));

        During(AwaitingStock,
            When(StockReserved)
                .TransitionTo(AwaitingPayment),
            When(StockFailed)
                .Activity(x => x.OfType<CancelOrderActivity>())
                .TransitionTo(Canceled),
            // Payment, StockReserved'ı saga'dan önce işleyip cevap verebilir: ödeme stok ayrıldıktan sonra yapıldığı için tamamlanır
            When(PaymentCompleted)
                .Activity(x => x.OfType<CompleteOrderActivity>())
                .TransitionTo(Completed),
            // Geçiş dönemi: OrderTimeoutWorker stok cevabı gelmemiş siparişler için de PaymentFailed yayınlar
            When(PaymentFailed)
                .Activity(x => x.OfType<CancelOrderActivity>())
                .TransitionTo(Canceled),
            Ignore(OrderCreated));

        During(AwaitingPayment,
            When(PaymentCompleted)
                .Activity(x => x.OfType<CompleteOrderActivity>())
                .TransitionTo(Completed),
            When(PaymentFailed)
                .Activity(x => x.OfType<CancelOrderActivity>())
                .TransitionTo(Canceled),
            Ignore(OrderCreated),
            Ignore(StockReserved),
            Ignore(StockFailed));

        // Terminal durumlar: kopya veya sıra dışı gelen event'ler sonucu değiştirmez
        During(Completed,
            Ignore(OrderCreated),
            Ignore(StockReserved),
            Ignore(StockFailed),
            Ignore(PaymentCompleted),
            Ignore(PaymentFailed));

        // Step 5: Canceled durumunda gelen PaymentCompleted için refund eklenecek
        During(Canceled,
            Ignore(OrderCreated),
            Ignore(StockReserved),
            Ignore(StockFailed),
            Ignore(PaymentCompleted),
            Ignore(PaymentFailed));
    }
}
