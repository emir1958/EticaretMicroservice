using EticaretMicroservice.Services.Order.Application.Interfaces;
using EticaretMicroservice.Shared.Events;
using MassTransit;
using Microsoft.Extensions.Logging;

namespace EticaretMicroservice.Services.Order.Application.Consumers;

public class StockFailedEventConsumer : IConsumer<StockFailedEvent>
{
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<StockFailedEventConsumer> _logger;

    public StockFailedEventConsumer(IOrderRepository orderRepository, ILogger<StockFailedEventConsumer> logger)
    {
        _orderRepository = orderRepository;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<StockFailedEvent> context)
    {
        var message = context.Message;
        _logger.LogWarning("StockFailedEvent alındı! OrderId: {OrderId}. Nedeni: {Message}", message.OrderId, message.Message);

        // 1. Siparişi veritabanından çek
        var order = await _orderRepository.GetByIdAsync(message.OrderId); // (Repository'e GetByIdAsync eklenmeli)

        if (order != null)
        {
            // 2. Sipariş durumunu 'Canceled' yap
            order.SetStatusToCanceled();

            // 3. Veritabanına kaydet
            await _orderRepository.SaveChangesAsync();
            _logger.LogInformation("OrderId: {OrderId} durumu 'Canceled' olarak güncellendi.", message.OrderId);
        }
    }
}