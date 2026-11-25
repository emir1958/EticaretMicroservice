using EticaretMicroservice.Payment.Api.Data;
using EticaretMicroservice.Payment.Api.Models;
using EticaretMicroservice.Payment.Api.Services;
using EticaretMicroservice.Shared.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace EticaretMicroservice.Payment.Api.Consumers;

public class StockReservedEventConsumer : IConsumer<StockReservedEvent>
{
    private readonly IPaymentService _paymentService;
    private readonly PaymentDbContext _dbContext;
    private readonly ILogger<StockReservedEventConsumer> _logger;

    public StockReservedEventConsumer(
        IPaymentService paymentService,
        PaymentDbContext dbContext,
        ILogger<StockReservedEventConsumer> logger)
    {
        _paymentService = paymentService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<StockReservedEvent> context)
    {
        var message = context.Message;

        // Başarılı ya da başarısız, bu sipariş için ödeme daha önce denendiyse tekrar çekilmez
        var existingPayment = await _dbContext.Payments
            .FirstOrDefaultAsync(x => x.OrderId == message.OrderId);

        if (existingPayment != null)
        {
            _logger.LogWarning(
                "[CorrelationId: {CorrelationId}] OrderId: {OrderId} için ödeme daha önce işlenmiş! Durum: {Status}. Mükerrer işlem engellendi.",
                message.CorrelationId, message.OrderId, existingPayment.Status);
            return;
        }

        _logger.LogInformation(
            "[CorrelationId: {CorrelationId}] Payment.API: StockReservedEvent alındı. OrderId: {OrderId}, Tutar: {Price} TL",
            message.CorrelationId, message.OrderId, message.TotalPrice);

        var (isSuccess, failReason) = _paymentService.ProcessPayment(message.PaymentToken, message.TotalPrice);

        if (isSuccess)
        {
            _dbContext.Payments.Add(new PaymentRecord
            {
                CorrelationId = message.CorrelationId,
                OrderId = message.OrderId,
                BuyerId = message.BuyerId,
                TotalPrice = message.TotalPrice,
                PaymentToken = message.PaymentToken,
                Status = PaymentStatus.Success,
                FailureReason = null,
                CreatedAt = DateTime.UtcNow
            });

            _logger.LogInformation(
                "[CorrelationId: {CorrelationId}] Ödeme ONAYLANDI! OrderId: {OrderId}",
                message.CorrelationId, message.OrderId);

            await context.Publish(new PaymentCompletedEvent
            {
                CorrelationId = message.CorrelationId,
                OrderId = message.OrderId,
                BuyerId = message.BuyerId,
                OrderItems = message.OrderItems
            });
        }
        else
        {
            // Başarısız deneme de kaydedilir: mesaj tekrar gelirse kart yeniden denenmez
            _dbContext.Payments.Add(new PaymentRecord
            {
                CorrelationId = message.CorrelationId,
                OrderId = message.OrderId,
                BuyerId = message.BuyerId,
                TotalPrice = message.TotalPrice,
                PaymentToken = message.PaymentToken,
                Status = PaymentStatus.Failed,
                FailureReason = failReason,
                CreatedAt = DateTime.UtcNow
            });

            _logger.LogWarning(
                "[CorrelationId: {CorrelationId}] Ödeme REDDEDİLDİ! OrderId: {OrderId}. Sebep: {Reason}",
                message.CorrelationId, message.OrderId, failReason);

            await context.Publish(new PaymentFailedEvent
            {
                CorrelationId = message.CorrelationId,
                OrderId = message.OrderId,
                BuyerId = message.BuyerId,
                Message = failReason ?? "Ödeme işlemi başarısız oldu.",
                OrderItems = message.OrderItems
            });
        }

        // PaymentRecord ve outbox'taki event tek SaveChanges ile aynı transaction'da yazılır
        await _dbContext.SaveChangesAsync();
    }
}