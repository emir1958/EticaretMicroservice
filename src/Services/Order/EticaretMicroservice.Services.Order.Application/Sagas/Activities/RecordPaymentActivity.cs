using EticaretMicroservice.Services.Order.Application.Interfaces;
using EticaretMicroservice.Shared.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EticaretMicroservice.Services.Order.Application.Sagas.Activities;

// İptal edilmiş siparişin ödeme tarafını kaydeder: geç gelen ödeme ve ardından yapılan iade. Sipariş durumu değişmez.
public class RecordPaymentActivity :
    IStateMachineActivity<OrderState, PaymentCompletedEvent>,
    IStateMachineActivity<OrderState, PaymentRefundedEvent>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<RecordPaymentActivity> _logger;

    public RecordPaymentActivity(IOrderRepository orderRepository, ILogger<RecordPaymentActivity> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task Execute(BehaviorContext<OrderState, PaymentCompletedEvent> context, IBehavior<OrderState, PaymentCompletedEvent> next)
    {
        var order = await GetOrderAsync(context.Saga);
        order.TryMarkPaymentReceived(context.Message.CreatedAt);

        await next.Execute(context);
    }

    public async Task Execute(BehaviorContext<OrderState, PaymentRefundedEvent> context, IBehavior<OrderState, PaymentRefundedEvent> next)
    {
        var order = await GetOrderAsync(context.Saga);

        if (!order.TryMarkRefunded(context.Message.CreatedAt))
        {
            _logger.LogWarning("[CorrelationId: {CorrelationId}] OrderId {OrderId} için iade kaydedilmedi, ödeme durumu: {PaymentStatus}",
                context.Saga.CorrelationId, order.Id, order.PaymentStatus);
        }

        await next.Execute(context);
    }

    private async Task<Domain.Entities.Order> GetOrderAsync(OrderState saga) =>
        await _orderRepository.GetByIdAsync(saga.OrderId)
            ?? throw new InvalidOperationException($"OrderId {saga.OrderId} için sipariş bulunamadı.");

    public Task Faulted<TException>(BehaviorExceptionContext<OrderState, PaymentCompletedEvent, TException> context,
        IBehavior<OrderState, PaymentCompletedEvent> next) where TException : Exception => next.Faulted(context);

    public Task Faulted<TException>(BehaviorExceptionContext<OrderState, PaymentRefundedEvent, TException> context,
        IBehavior<OrderState, PaymentRefundedEvent> next) where TException : Exception => next.Faulted(context);

    public void Probe(ProbeContext context) => context.CreateScope("record-payment");

    public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);
}
