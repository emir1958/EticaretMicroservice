using EticaretMicroservice.Services.Order.Application.Sagas.Activities;
using EticaretMicroservice.Shared.Commands;
using EticaretMicroservice.Shared.Events;
using MassTransit;
using Microsoft.Extensions.Options;

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

    public Schedule<OrderState, PaymentTimeoutExpired> PaymentTimeout { get; private set; } = null!;

    public OrderStateMachine(IOptions<OrderSagaOptions> options)
    {
        var paymentTimeout = options.Value.PaymentTimeout;

        InstanceState(x => x.CurrentState);

        Event(() => OrderCreated, x => x.CorrelateById(ctx => ctx.Message.CorrelationId));

        // Stock/Payment cevabı, saga OrderCreated'ı işlemeden önce gelebilir (paralel kuyruklar).
        // Instance yoksa mesaj hata sayılır ve retry ile instance oluştuktan sonra yeniden işlenir.
        Event(() => StockReserved, x => { x.CorrelateById(ctx => ctx.Message.CorrelationId); x.OnMissingInstance(m => m.Fault()); });
        Event(() => StockFailed, x => { x.CorrelateById(ctx => ctx.Message.CorrelationId); x.OnMissingInstance(m => m.Fault()); });
        Event(() => PaymentCompleted, x => { x.CorrelateById(ctx => ctx.Message.CorrelationId); x.OnMissingInstance(m => m.Fault()); });
        Event(() => PaymentFailed, x => { x.CorrelateById(ctx => ctx.Message.CorrelationId); x.OnMissingInstance(m => m.Fault()); });

        // Zamanlanmış mesaj saga'nın kendi kuyruğuna gelir (RabbitMQ'da delayed exchange). Token PaymentTimeoutTokenId'de tutulur;
        // Unschedule token'ı temizler, bu yüzden iptal edilemeyen (delayed exchange iptal desteklemez) eski mesajlar
        // MassTransit tarafından "güncel değil" sayılıp Received event'ine dönüştürülmez.
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

        // Ödeme süresi doldu. Stok cevabı hiç gelmemiş siparişler de iptal edilir (eski OrderTimeoutWorker davranışı).
        During(AwaitingStock, AwaitingPayment,
            // ExpiresAt emniyet kontrolü: süre henüz dolmadıysa (erken teslim, node'lar arası saat farkı) iptal edilmez,
            // kalan süre için yeniden zamanlanır.
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

        // Terminal durumlar: kopya, sıra dışı veya geç gelen event'ler sonucu değiştirmez
        During(Completed,
            Ignore(OrderCreated),
            Ignore(StockReserved),
            Ignore(StockFailed),
            Ignore(PaymentCompleted),
            Ignore(PaymentFailed),
            Ignore(PaymentTimeout.Received));

        During(Canceled,
            // Sipariş iptal edildikten sonra (örn. timeout) ödeme tamamlandı: müşteriden çekilen para iade edilir, sipariş iptal kalır.
            // Payment iadeyi yalnızca Success durumundaki ödeme için yapar; kopya komut zararsızdır.
            When(PaymentCompleted)
                .PublishAsync(ctx => ctx.Init<RefundPaymentCommand>(new
                {
                    ctx.Saga.CorrelationId,
                    ctx.Saga.OrderId,
                    Reason = "Sipariş iptal edildikten sonra ödeme alındı."
                })),
            // Timeout stok cevabından önce gelirse Stock rezervasyonu iptalden sonra yapmış olabilir: tekrar iade istenir.
            // Stock tarafında iade siparişin aktif rezervasyonlarına göre yapıldığı için kopya istek zararsızdır.
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

    // ExpiresAt'i olmayan instance (Step 4 öncesinde oluşmuş) için timeout'a güvenilir
    private static bool IsExpired(OrderState saga) => saga.ExpiresAt is null || saga.ExpiresAt <= DateTime.UtcNow;
}
