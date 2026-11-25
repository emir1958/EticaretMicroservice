using EticaretMicroservice.Services.Order.Application.Sagas.Activities;
using EticaretMicroservice.Shared.Commands;
using EticaretMicroservice.Shared.Events;
using MassTransit;
using Microsoft.Extensions.Options;

namespace EticaretMicroservice.Services.Order.Application.Sagas;

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
    public Event<PaymentRefundedEvent> PaymentRefunded { get; private set; } = null!;

    public Schedule<OrderState, PaymentTimeoutExpired> PaymentTimeout { get; private set; } = null!;

    public OrderStateMachine(IOptions<OrderSagaOptions> options)
    {
        var paymentTimeout = options.Value.PaymentTimeout;

        InstanceState(x => x.CurrentState);

        Event(() => OrderCreated, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));

      
        Event(() => StockReserved, x => { x.CorrelateById(ctx => ctx.Message.CorrelationId); x.OnMissingInstance(m => m.Fault()); });
        Event(() => StockFailed, x => { x.CorrelateById(ctx => ctx.Message.CorrelationId); x.OnMissingInstance(m => m.Fault()); });
        Event(() => PaymentCompleted, x => { x.CorrelateById(ctx => ctx.Message.CorrelationId); x.OnMissingInstance(m => m.Fault()); });
        Event(() => PaymentFailed, x => { x.CorrelateById(ctx => ctx.Message.CorrelationId); x.OnMissingInstance(m => m.Fault()); });
        Event(() => PaymentRefunded, x => { x.CorrelateById(ctx => ctx.Message.CorrelationId); x.OnMissingInstance(m => m.Fault()); });

       
        Schedule(() => PaymentTimeout, x => x.PaymentTimeoutTokenId, s =>
        {
            s.Delay = paymentTimeout;
            s.Received = r => { r.CorrelateById(ctx => ctx.Message.CorrelationId); r.OnMissingInstance(m => m.Discard()); };
        });

        Initially(
            When(OrderCreated)
                .Then(ctx =>
                {
                    ctx.Saga.OrderId = ctx.Message.OrderId;
                    ctx.Saga.BuyerId = ctx.Message.BuyerId;
                    ctx.Saga.CreatedAt = DateTime.UtcNow;
                    ctx.Saga.ExpiresAt = ctx.Saga.CreatedAt + paymentTimeout;
                })
                .Schedule(PaymentTimeout, ctx => new PaymentTimeoutExpired { CorrelationId = ctx.Saga.CorrelationId })
                .TransitionTo(AwaitingStock));

        During(AwaitingStock,
            When(StockReserved)
                .Activity(x => x.OfType<RecordStockReservedActivity>())
                .TransitionTo(AwaitingPayment),
            When(StockFailed)
                .Unschedule(PaymentTimeout)
                .Activity(x => x.OfType<CancelOrderActivity>())
                .TransitionTo(Canceled),
            // Payment, StockReserved'ı saga'dan önce işleyip cevap verebilir: ödeme stok ayrıldıktan sonra yapıldığı için
            // tamamlanır / başarısız olur (Stock, PaymentFailedEvent'i kendisi dinleyip rezervasyonu iade eder).
            When(PaymentCompleted)
                .Unschedule(PaymentTimeout)
                .Activity(x => x.OfType<CompleteOrderActivity>())
                .TransitionTo(Completed),
            When(PaymentFailed)
                .Unschedule(PaymentTimeout)
                .Activity(x => x.OfType<CancelOrderActivity>())
                .TransitionTo(Canceled),
            Ignore(OrderCreated));

        During(AwaitingPayment,
            When(PaymentCompleted)
                .Unschedule(PaymentTimeout)
                .Activity(x => x.OfType<CompleteOrderActivity>())
                .TransitionTo(Completed),
            When(PaymentFailed)
                .Unschedule(PaymentTimeout)
                .Activity(x => x.OfType<CancelOrderActivity>())
                .TransitionTo(Canceled),
            Ignore(OrderCreated),
            Ignore(StockReserved),
            Ignore(StockFailed));


        During(AwaitingStock, AwaitingPayment,
            When(PaymentTimeout.Received, ctx => !IsExpired(ctx.Saga))
                .Schedule(PaymentTimeout, ctx => new PaymentTimeoutExpired { CorrelationId = ctx.Saga.CorrelationId },
                    ctx => ctx.Saga.ExpiresAt!.Value),
            When(PaymentTimeout.Received, ctx => IsExpired(ctx.Saga))
                .Activity(x => x.OfType<CancelOrderActivity>())
                .PublishAsync(ctx => ctx.Init<ReleaseStockCommand>(new
                {
                    ctx.Saga.CorrelationId,
                    ctx.Saga.OrderId,
                    Reason = "Ödeme süresi doldu."
                }))
                .TransitionTo(Canceled));

        During(Completed,
            // Ödeme stoktan önce işlendiyse stok sonucu buraya gelir: durum değişmez, yalnızca geçmişe yazılır
            When(StockReserved)
                .Activity(x => x.OfType<RecordStockReservedActivity>()),
            Ignore(OrderCreated),
            Ignore(StockFailed),
            Ignore(PaymentCompleted),
            Ignore(PaymentFailed),
            Ignore(PaymentTimeout.Received));

        During(Canceled,
            When(PaymentCompleted)
                .Activity(x => x.OfType<RecordPaymentActivity>())
                .PublishAsync(ctx => ctx.Init<RefundPaymentCommand>(new
                {
                    ctx.Saga.CorrelationId,
                    ctx.Saga.OrderId,
                    Reason = "Sipariş iptal edildikten sonra ödeme alındı."
                })),
            When(PaymentRefunded)
                .Activity(x => x.OfType<RecordPaymentActivity>()),
            When(StockReserved)
                .PublishAsync(ctx => ctx.Init<ReleaseStockCommand>(new
                {
                    ctx.Saga.CorrelationId,
                    ctx.Saga.OrderId,
                    Reason = "Sipariş iptal edildikten sonra stok rezerve edildi."
                })),
            Ignore(OrderCreated),
            Ignore(StockFailed),
            Ignore(PaymentFailed),
            Ignore(PaymentTimeout.Received));
    }

    private static bool IsExpired(OrderState saga) => saga.ExpiresAt is null || saga.ExpiresAt <= DateTime.UtcNow;
}
