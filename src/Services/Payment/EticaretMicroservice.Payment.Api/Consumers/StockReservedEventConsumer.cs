using EticaretMicroservice.Payment.Api.Data;
using EticaretMicroservice.Payment.Api.Services;
using EticaretMicroservice.Shared.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace EticaretMicroservice.Payment.Api.Consumers;

public class StockReservedEventConsumer : IConsumer<StockReservedEvent>
{
    private readonly IPaymentService _paymentService;
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly PaymentDbContext _dbContext;
    private readonly ILogger<StockReservedEventConsumer> _logger;

    public StockReservedEventConsumer(
        IPaymentService paymentService,
        IPublishEndpoint publishEndpoint,
        PaymentDbContext dbContext,
        ILogger<StockReservedEventConsumer> logger)
    {
        _paymentService = paymentService;
        _publishEndpoint = publishEndpoint;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<StockReservedEvent> context)
    {
        var message = context.Message;

        // 1. Veritabanı Seviyesinde Idempotency Kontrolü
        var alreadyPaid = await _dbContext.Payments.AnyAsync(x => x.OrderId == message.OrderId);
        if (alreadyPaid)
        {
            _logger.LogWarning("[CorrelationId: {CorrelationId}] OrderId {OrderId} için ödeme veritabanında zaten mevcut. Mükerrer çekim engellendi.",
                message.CorrelationId, message.OrderId);
            return;
        }

        _logger.LogInformation("[CorrelationId: {CorrelationId}] Payment.API: StockReservedEvent alındı. OrderId: {OrderId}, Tutar: {Price} TL",
            message.CorrelationId, message.OrderId, message.TotalPrice);

        // 2. Ödeme Çekimi
        var (isSuccess, failReason) = _paymentService.ProcessPayment(message.PaymentToken, message.TotalPrice);

        if (isSuccess)
        {
            // Ödeme kaydı eklenir
            _dbContext.Payments.Add(new PaymentRecord
            {
                OrderId = message.OrderId,
                BuyerId = message.BuyerId,
                TotalPrice = message.TotalPrice,
                PaymentToken = message.PaymentToken,
                CreatedAt = DateTime.UtcNow
            });

            _logger.LogInformation("[CorrelationId: {CorrelationId}] Ödeme ONAYLANDI! OrderId: {OrderId}",
                message.CorrelationId, message.OrderId);

            await _publishEndpoint.Publish(new PaymentCompletedEvent
            {
                CorrelationId = message.CorrelationId,
                OrderId = message.OrderId,
                BuyerId = message.BuyerId,
                OrderItems = message.OrderItems // Stock confirm işlemi için aktarılır
            });

            // Payment kaydı ve Outbox mesajı tek SQL Transaction ile commit edilir
            await _dbContext.SaveChangesAsync();
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

            await _dbContext.SaveChangesAsync();
        }
    }
}