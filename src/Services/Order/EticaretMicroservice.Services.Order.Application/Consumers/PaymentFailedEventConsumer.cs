using EticaretMicroservice.Services.Order.Application.Hubs;
using EticaretMicroservice.Services.Order.Application.Interfaces;
using EticaretMicroservice.Shared.Events;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace EticaretMicroservice.Services.Order.Application.Consumers;

public class PaymentFailedEventConsumer : IConsumer<PaymentFailedEvent>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IHubContext<OrderHub> _hubContext; // 🔹 SignalR Hub Context
    private readonly ILogger<PaymentFailedEventConsumer> _logger;

    public PaymentFailedEventConsumer(IOrderRepository orderRepository, ILogger<PaymentFailedEventConsumer> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentFailedEvent> context)
    {
        var message = context.Message;
        _logger.LogWarning("PaymentFailedEvent yakalandı! OrderId: {OrderId}, Nedeni: {Reason}", message.OrderId, message.Message);

        var order = await _orderRepository.GetByIdAsync(message.OrderId);
        if (order != null)
        {
            order.SetStatusToCanceled();
            await _orderRepository.SaveChangesAsync();
            _logger.LogInformation("OrderId: {OrderId} ödeme hatası nedeniyle 'Canceled' durumuna çekildi.", message.OrderId);
            await _hubContext.Clients.Group(message.BuyerId).SendAsync("ReceiveOrderState", new
            {
                OrderId = message.OrderId,
                Status = "Canceled",
                Message = $"Ödeme Başarısız: {message.Message}"
            });
        }
    }
}