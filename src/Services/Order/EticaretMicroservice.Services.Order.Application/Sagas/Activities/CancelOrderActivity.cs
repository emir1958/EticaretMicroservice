using EticaretMicroservice.Services.Order.Application.Interfaces;
using EticaretMicroservice.Shared.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EticaretMicroservice.Services.Order.Application.Sagas.Activities;

// Stok yetersizliği, ödeme hatası veya ödeme süresinin dolması nedeniyle Order aggregate'ini iptal eder.
public class CancelOrderActivity :
    IStateMachineActivity<OrderState, StockFailedEvent>,
    IStateMachineActivity<OrderState, PaymentFailedEvent>,
    IStateMachineActivity<OrderState, PaymentTimeoutExpired>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderNotifier _notifier;
    private readonly ILogger<CancelOrderActivity> _logger;

    public CancelOrderActivity(IOrderRepository orderRepository, IOrderNotifier notifier, ILogger<CancelOrderActivity> logger)
    {
        _orderRepository = orderRepository;
        _notifier = notifier;
        _logger = logger;
    }

    public async Task Execute(BehaviorContext<OrderState, StockFailedEvent> context, IBehavior<OrderState, StockFailedEvent> next)
    {
        var reason = string.IsNullOrWhiteSpace(context.Message.Message)
            ? "Siparişteki bir veya daha fazla ürün için yeterli stok bulunamadı."
            : context.Message.Message;
        var failedAt = context.Message.CreatedAt;

        var order = await GetOrderAsync(context.Saga);
        order.TryMarkStockFailed(failedAt, reason);
        await CancelAsync(context.Saga, order, failedAt, note: null, reason, context.CancellationToken);

        await next.Execute(context);
    }

    public async Task Execute(BehaviorContext<OrderState, PaymentFailedEvent> context, IBehavior<OrderState, PaymentFailedEvent> next)
    {
        var failedAt = context.Message.CreatedAt;

        var order = await GetOrderAsync(context.Saga);
        order.TryMarkPaymentFailed(failedAt, context.Message.Message);
        await CancelAsync(context.Saga, order, failedAt, note: null, $"Ödeme Başarısız: {context.Message.Message}", context.CancellationToken);

        await next.Execute(context);
    }

    public async Task Execute(BehaviorContext<OrderState, PaymentTimeoutExpired> context, IBehavior<OrderState, PaymentTimeoutExpired> next)
    {
        var order = await GetOrderAsync(context.Saga);
        await CancelAsync(context.Saga, order, DateTime.UtcNow, "Ödeme süresi doldu.",
            "Siparişiniz zaman aşımı nedeniyle iptal edildi.", context.CancellationToken);

        await next.Execute(context);
    }

    private async Task<Domain.Entities.Order> GetOrderAsync(OrderState saga) =>
        await _orderRepository.GetByIdAsync(saga.OrderId)
            ?? throw new InvalidOperationException($"OrderId {saga.OrderId} için sipariş bulunamadı.");

    // Sebep (stok/ödeme) ayrı kayıt olarak eklendiği için iptal kaydına not yalnızca zaman aşımında yazılır
    private async Task CancelAsync(OrderState saga, Domain.Entities.Order order, DateTime canceledAt, string? note,
        string message, CancellationToken cancellationToken)
    {
        if (!order.TrySetStatusToCanceled(canceledAt, note))
        {
            // Order'ı yalnızca saga değiştirir; buraya düşmek saga ile Order'ın ayrıştığını gösterir
            _logger.LogWarning("[CorrelationId: {CorrelationId}] OrderId {OrderId} zaten '{Status}' durumunda.",
                saga.CorrelationId, order.Id, order.OrderStatus);
            return;
        }

        await _notifier.NotifyOrderStateAsync(saga.BuyerId, order.Id, "Canceled", message, cancellationToken);
    }

    public Task Faulted<TException>(BehaviorExceptionContext<OrderState, StockFailedEvent, TException> context,
        IBehavior<OrderState, StockFailedEvent> next) where TException : Exception => next.Faulted(context);

    public Task Faulted<TException>(BehaviorExceptionContext<OrderState, PaymentFailedEvent, TException> context,
        IBehavior<OrderState, PaymentFailedEvent> next) where TException : Exception => next.Faulted(context);

    public Task Faulted<TException>(BehaviorExceptionContext<OrderState, PaymentTimeoutExpired, TException> context,
        IBehavior<OrderState, PaymentTimeoutExpired> next) where TException : Exception => next.Faulted(context);

    public void Probe(ProbeContext context) => context.CreateScope("cancel-order");

    public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);
}
