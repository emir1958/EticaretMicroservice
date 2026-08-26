using EticaretMicroservice.Shared.Events;
using MassTransit;

namespace EticaretMicroservice.Payment.Api.Consumers;

public class StockReservedEventConsumer : IConsumer<StockReservedEvent>
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<StockReservedEventConsumer> _logger;

    public StockReservedEventConsumer(
        IPublishEndpoint publishEndpoint,
        ILogger<StockReservedEventConsumer> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<StockReservedEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Payment.API: StockReservedEvent alındı. OrderId: {OrderId}, Tutar: {Price} TL, Token: {Token}",
            message.OrderId, message.TotalPrice, message.PaymentToken);

        // 🟢 GÜVENLİK DÜZELTMESİ: Ödeme token üzerinden simüle ediliyor
        bool isSuccess = !string.IsNullOrEmpty(message.PaymentToken);

        if (isSuccess)
        {
            _logger.LogInformation("Ödeme BANKADAN ONAYLANDI! OrderId: {OrderId}", message.OrderId);

            await _publishEndpoint.Publish(new PaymentCompletedEvent
            {
                OrderId = message.OrderId,
                BuyerId = message.BuyerId
            });
        }
        else
        {
            _logger.LogWarning("Ödeme REDDEDİLDİ! OrderId: {OrderId}", message.OrderId);

            await _publishEndpoint.Publish(new PaymentFailedEvent
            {
                OrderId = message.OrderId,
                BuyerId = message.BuyerId,
                Message = "Geçersiz Ödeme Tokenı.",
                OrderItems = message.OrderItems
            });
        }
    }
}