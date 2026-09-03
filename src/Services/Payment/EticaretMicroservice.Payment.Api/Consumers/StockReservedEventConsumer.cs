using System.Collections.Concurrent;
using EticaretMicroservice.Payment.Api.Services;
using EticaretMicroservice.Shared.Events;
using MassTransit;

namespace EticaretMicroservice.Payment.Api.Consumers;

public class StockReservedEventConsumer : IConsumer<StockReservedEvent>
{
    private readonly IPaymentService _paymentService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<StockReservedEventConsumer> _logger;

    // Idempotency: İşlenmiş OrderId kayıtları
    private static readonly ConcurrentDictionary<int, bool> ProcessedOrders = new();

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

        // 1. Idempotency Kontrolü
        if (ProcessedOrders.ContainsKey(message.OrderId))
        {
            _logger.LogWarning("[CorrelationId: {CorrelationId}] OrderId {OrderId} için ödeme daha önce işlenmiş, mükerrer çekim engellendi.",
                message.CorrelationId, message.OrderId);
            return;
        }

        _logger.LogInformation("[CorrelationId: {CorrelationId}] Payment.API: StockReservedEvent alındı. OrderId: {OrderId}, Tutar: {Price} TL",
            message.CorrelationId, message.OrderId, message.TotalPrice);

        // 2. Ödeme İşlemi
        var (isSuccess, failReason) = _paymentService.ProcessPayment(message.PaymentToken, message.TotalPrice);

        if (isSuccess)
        {
            ProcessedOrders.TryAdd(message.OrderId, true);

            _logger.LogInformation("[CorrelationId: {CorrelationId}] Ödeme ONAYLANDI! OrderId: {OrderId}",
                message.CorrelationId, message.OrderId);

            await _publishEndpoint.Publish(new PaymentCompletedEvent
            {
                CorrelationId = message.CorrelationId,
                OrderId = message.OrderId,
                BuyerId = message.BuyerId
            });
        }
        else
        {
            _logger.LogWarning("[CorrelationId: {CorrelationId}] Ödeme REDDEDİLDİ! OrderId: {OrderId}. Sebep: {Reason}",
                message.CorrelationId, message.OrderId, failReason);

            await _publishEndpoint.Publish(new PaymentFailedEvent
            {
                CorrelationId = message.CorrelationId,
                OrderId = message.OrderId,
                BuyerId = message.BuyerId,
                Message = failReason ?? "Ödeme işlemi başarısız oldu.",
                OrderItems = message.OrderItems
            });
        }
    }
}