using EticaretMicroservice.Services.Order.Application.Hubs;
using EticaretMicroservice.Services.Order.Application.Interfaces;
using EticaretMicroservice.Shared.Events;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

namespace EticaretMicroservice.Services.Order.Application.Consumers;

public class StockFailedEventConsumer : IConsumer<StockFailedEvent>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IHubContext<OrderHub> _hubContext; // 🟢 1. SignalR Hub Enjeksiyonu
    private readonly ILogger<StockFailedEventConsumer> _logger;

    public StockFailedEventConsumer(
        IOrderRepository orderRepository,
        IHubContext<OrderHub> hubContext, // 🟢 2. Constructor'a eklendi
        ILogger<StockFailedEventConsumer> logger)
    {
        _orderRepository = orderRepository;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<StockFailedEvent> context)
    {
        var message = context.Message;
        _logger.LogWarning("StockFailedEvent alındı! OrderId: {OrderId}. Nedeni: {Message}", message.OrderId, message.Message);

        // 1. Siparişi veritabanından çek
        var order = await _orderRepository.GetByIdAsync(message.OrderId);

        if (order != null)
        {
            // 2. Sipariş durumunu 'Canceled' yap
            order.SetStatusToCanceled();

            // 3. Veritabanına kaydet
            await _orderRepository.SaveChangesAsync();
            _logger.LogInformation("OrderId: {OrderId} durumu 'Canceled' olarak güncellendi.", message.OrderId);

            // 🟢 4. KRİTİK: Kullanıcının ekranına SignalR ile İptal durumunu fırlat
            await _hubContext.Clients.Group(order.BuyerId).SendAsync("ReceiveOrderState", new
            {
                OrderId = order.Id,
                Status = "Canceled",
                Message = message.Message ?? "Siparişteki bir veya daha fazla ürün için yeterli stok bulunamadı."
            });
        }
    }
}