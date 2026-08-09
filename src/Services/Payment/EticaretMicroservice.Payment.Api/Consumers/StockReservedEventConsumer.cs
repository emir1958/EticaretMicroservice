using EticaretMicroservice.Payment.Api.Services;
using EticaretMicroservice.Shared.Events;
using MassTransit;

namespace EticaretMicroservice.Payment.Api.Consumers;

public class StockReservedEventConsumer : IConsumer<StockReservedEvent>
{
    private readonly IPaymentService _paymentService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<StockReservedEventConsumer> _logger;

    public StockReservedEventConsumer(
        IPaymentService paymentService,
        IPublishEndpoint publishEndpoint,
        ILogger<StockReservedEventConsumer> logger)
    {
        _paymentService = paymentService;
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<StockReservedEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Payment.API: StockReservedEvent alındı. OrderId: {OrderId}, Tutar: {Price} TL", message.OrderId, message.TotalPrice);

        // 🟢 Gerçekçi Banka / Kart Doğrulaması Çalıştırılıyor
        var (isSuccess, failReason) = _paymentService.ProcessPayment(message.Payment, message.TotalPrice);

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
            _logger.LogWarning("Ödeme REDDEDİLDİ! OrderId: {OrderId}, Nedeni: {Reason}", message.OrderId, failReason);

            // 🔴 Ödeme Başarısız -> Hem Order (Sipariş İptal) hem Stock (Stok İade) için Event fırlatılıyor!
            await _publishEndpoint.Publish(new PaymentFailedEvent
            {
                OrderId = message.OrderId,
                BuyerId = message.BuyerId,
                Message = failReason
            });
        }
    }
}