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
    private readonly IHubContext<OrderHub> _hubContext;
    private readonly ILogger<PaymentFailedEventConsumer> _logger;

    public PaymentFailedEventConsumer(
        IOrderRepository orderRepository,
        IHubContext<OrderHub> hubContext,
        ILogger<PaymentFailedEventConsumer> logger)
    {
        _orderRepository = orderRepository;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentFailedEvent> context)
    {
        var message = context.Message;
        _logger.LogWarning("[CorrelationId: {CorrelationId}] PaymentFailedEvent yakalandı! OrderId: {OrderId}, Nedeni: {Reason}",
            message.CorrelationId, message.OrderId, message.Message);

        var order = await _orderRepository.GetByIdAsync(message.OrderId);
        if (order == null)
        {
            _logger.LogWarning("OrderId: {OrderId} veritabanında bulunamadı, işlem atlanıyor.", message.OrderId);
            return;
        }

        var isStatusChanged = order.TrySetStatusToCanceled();

        if (!isStatusChanged)
        {
            _logger.LogInformation("OrderId: {OrderId} zaten '{Status}' durumunda olduğu için iptal adımı atlandı (Mükerrer event koruması).",
                order.Id, order.OrderStatus);
            return;
        }

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