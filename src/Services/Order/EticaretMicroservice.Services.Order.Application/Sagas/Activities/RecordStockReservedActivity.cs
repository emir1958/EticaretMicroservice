using EticaretMicroservice.Services.Order.Application.Interfaces;
using EticaretMicroservice.Shared.Events;
using MassTransit;

namespace EticaretMicroservice.Services.Order.Application.Sagas.Activities;

// Stok ayrıldığını siparişin geçmişine yazar; sipariş durumu ödeme gelene kadar değişmez.
public class RecordStockReservedActivity : IStateMachineActivity<OrderState, StockReservedEvent>
{
    private readonly IOrderRepository _orderRepository;

    public RecordStockReservedActivity(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task Execute(BehaviorContext<OrderState, StockReservedEvent> context, IBehavior<OrderState, StockReservedEvent> next)
    {
        var order = await _orderRepository.GetByIdAsync(context.Saga.OrderId)
            ?? throw new InvalidOperationException($"OrderId {context.Saga.OrderId} için sipariş bulunamadı.");

        order.TryMarkStockReserved(context.Message.CreatedAt);

        await next.Execute(context);
    }

    public Task Faulted<TException>(BehaviorExceptionContext<OrderState, StockReservedEvent, TException> context,
        IBehavior<OrderState, StockReservedEvent> next) where TException : Exception => next.Faulted(context);

    public void Probe(ProbeContext context) => context.CreateScope("record-stock-reserved");

    public void Accept(StateMachineVisitor visitor) => visitor.Visit(this);
}
