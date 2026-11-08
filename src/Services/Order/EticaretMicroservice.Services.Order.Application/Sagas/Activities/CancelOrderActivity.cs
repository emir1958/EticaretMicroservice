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
        await CancelAsync(context.Saga,
            string.IsNullOrWhiteSpace(context.Message.Message)
                ? "Siparişteki bir veya daha fazla ürün için yeterli stok bulunamadı."
                : context.Message.Message,
            context.CancellationToken);

        await next.Execute(context);
    }

    public async Task Execute(BehaviorContext<OrderState, PaymentFailedEvent> context, IBehavior<OrderState, PaymentFailedEvent> next)
    {
        await CancelAsync(context.Saga, $"Ödeme Başarısız: {context.Message.Message}", context.CancellationToken);

        await next.Execute(context);
    }

    public async Task Execute(BehaviorContext<OrderState, PaymentTimeoutExpired> context, IBehavior<OrderState, PaymentTimeoutExpired> next)
    {
        await CancelAsync(context.Saga, "Siparişiniz zaman aşımı nedeniyle iptal edildi.", context.CancellationToken);

        await next.Execute(context);
    }

    private async Task CancelAsync(OrderState saga, string message, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(saga.OrderId)
            ?? throw new InvalidOperationException($"OrderId {saga.OrderId} için sipariş bulunamadı.");

        if (!order.TrySetStatusToCanceled())
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
