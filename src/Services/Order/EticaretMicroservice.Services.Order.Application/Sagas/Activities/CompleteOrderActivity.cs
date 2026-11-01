using EticaretMicroservice.Services.Order.Application.Interfaces;
using EticaretMicroservice.Shared.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EticaretMicroservice.Services.Order.Application.Sagas.Activities;

// Ödeme onaylandığında Order aggregate'ini tamamlar.
// Order değişikliği saga durumuyla aynı OrderDbContext üzerinden, tek transaction'da kaydedilir.
public class CompleteOrderActivity : IStateMachineActivity<OrderState, PaymentCompletedEvent>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IOrderNotifier _notifier;
    private readonly ILogger<CompleteOrderActivity> _logger;

    public CompleteOrderActivity(IOrderRepository orderRepository, IOrderNotifier notifier, ILogger<CompleteOrderActivity> logger)
    {
        _orderRepository = orderRepository;
        _notifier = notifier;
        _logger = logger;
    }

    public async Task Execute(BehaviorContext<OrderState, PaymentCompletedEvent> context, IBehavior<OrderState, PaymentCompletedEvent> next)
    {
        var order = await _orderRepository.GetByIdAsync(context.Saga.OrderId)
            ?? throw new InvalidOperationException($"OrderId {context.Saga.OrderId} için sipariş bulunamadı.");

        if (order.TrySetStatusToCompleted())
        {
            await _notifier.NotifyOrderStateAsync(context.Saga.BuyerId, order.Id, "Completed",
                "Siparişiniz ve ödemeniz başarıyla onaylandı!", context.CancellationToken);
        }
        else
        {
            // Geçiş dönemi: OrderTimeoutWorker siparişi saga'dan bağımsız iptal edebiliyor (Step 4'te kaldırılacak)
            _logger.LogWarning("[CorrelationId: {CorrelationId}] OrderId {OrderId} tamamlanamadı, mevcut durum: {Status}",
                context.Saga.CorrelationId, order.Id, order.OrderStatus);
        }

        await next.Execute(context);
    }

    public Task Faulted<TException>(BehaviorExceptionContext<OrderState, PaymentCompletedEvent, TException> context,
        IBehavior<OrderState, PaymentCompletedEvent> next) where TException : Exception => next.Faulted(context);

    public void Probe(ProbeContext context) => context.CreateScope("complete-order");

    public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);
}
