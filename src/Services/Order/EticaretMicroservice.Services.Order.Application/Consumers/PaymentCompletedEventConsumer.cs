using EticaretMicroservice.Services.Order.Application.Hubs;
using EticaretMicroservice.Services.Order.Application.Interfaces;
using EticaretMicroservice.Shared.Events;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EticaretMicroservice.Services.Order.Application.Consumers;

public class PaymentCompletedEventConsumer : IConsumer<PaymentCompletedEvent>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IHubContext<OrderHub> _hubContext;
    private readonly ILogger<PaymentCompletedEventConsumer> _logger;

    public PaymentCompletedEventConsumer(
        IOrderRepository orderRepository,
        IHubContext<OrderHub> hubContext,
        ILogger<PaymentCompletedEventConsumer> logger)
    {
        _orderRepository = orderRepository;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<PaymentCompletedEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("PaymentCompletedEvent yakalandı! OrderId: {OrderId}", message.OrderId);

        var order = await _orderRepository.GetByIdAsync(message.OrderId);
        if (order == null)
        {
            _logger.LogWarning("OrderId: {OrderId} bulunamadı!", message.OrderId);
            return;
        }

        // 🟢 1. Katı Durum Kuralı: Sadece 'Beklemede' olan sipariş tamamlanabilir
        var isSuccess = order.TrySetStatusToCompleted();
        if (!isSuccess)
        {
            _logger.LogWarning("OrderId: {OrderId} durumu 'Beklemede' olmadığı için tamamlanamadı. Güncel durum: {Status}",
                order.Id, order.OrderStatus);
            return;
        }

        try
        {
            // 🟢 2. RowVersion kontrolü SaveChangesAsync sırasında EF Core tarafından otomatik yapılır
            await _orderRepository.SaveChangesAsync();

            _logger.LogInformation("OrderId: {OrderId} ödemesi onaylandı. Durum 'Completed' yapıldı.", message.OrderId);

            await _hubContext.Clients.Group(message.BuyerId).SendAsync("ReceiveOrderState", new
            {
                OrderId = message.OrderId,
                Status = "Completed",
                Message = "Siparişiniz ve ödemeniz başarıyla onaylandı!"
            });
        }
        catch (DbUpdateConcurrencyException ex)
        {
            _logger.LogError(ex, "OrderId: {OrderId} güncellenirken Concurrency Conflict oluştu! Sipariş başka bir işlem (ör. Timeout) tarafından değiştirilmiş.", message.OrderId);
        }
    }
}