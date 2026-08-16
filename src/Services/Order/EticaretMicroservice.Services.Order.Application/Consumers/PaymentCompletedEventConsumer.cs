using EticaretMicroservice.Services.Order.Application.Hubs;
using EticaretMicroservice.Services.Order.Application.Interfaces;
using EticaretMicroservice.Shared.Events;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace EticaretMicroservice.Services.Order.Application.Consumers;

public class PaymentCompletedEventConsumer : IConsumer<PaymentCompletedEvent>
{
    private readonly IOrderRepository _orderRepository;

    private readonly IHubContext<OrderHub> _hubContext; // 🔹 SignalR Hub Context
    private readonly ILogger<PaymentCompletedEventConsumer> _logger;

    public PaymentCompletedEventConsumer(IOrderRepository orderRepository, ILogger<PaymentCompletedEventConsumer> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentCompletedEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("PaymentCompletedEvent yakalandı! OrderId: {OrderId}", message.OrderId);

        var order = await _orderRepository.GetByIdAsync(message.OrderId);
        if (order != null)
        {
            order.SetStatusToCompleted();
            await _orderRepository.SaveChangesAsync();
            _logger.LogInformation("OrderId: {OrderId} ödemesi onaylandı. Durum 'Completed' yapıldı.", message.OrderId);
            await _hubContext.Clients.Group(message.BuyerId).SendAsync("ReceiveOrderState", new
            {
                OrderId = message.OrderId,
                Status = "Completed",
                Message = "Siparişiniz ve ödemeniz başarıyla onaylandı!"
            });
        }
    }
}