using EticaretMicroservice.Payment.Api.Data;
using EticaretMicroservice.Payment.Api.Models;
using EticaretMicroservice.Payment.Api.Services;
using EticaretMicroservice.Shared.Commands;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace EticaretMicroservice.Payment.Api.Consumers;

// Order saga'sı, iptal edilmiş (örn. ödeme süresi dolmuş) bir sipariş için ödeme tamamlandığında gönderir.
// İdempotent: yalnızca Success durumundaki ödeme iade edilir; Refunded/Failed ya da kayıt yoksa hiçbir şey yapılmaz.
public class RefundPaymentCommandConsumer : IConsumer<RefundPaymentCommand>
{
    private readonly IPaymentService _paymentService;
    private readonly PaymentDbContext _dbContext;
    private readonly ILogger<RefundPaymentCommandConsumer> _logger;

    public RefundPaymentCommandConsumer(
        IPaymentService paymentService,
        PaymentDbContext dbContext,
        ILogger<RefundPaymentCommandConsumer> logger)
    {
        _paymentService = paymentService;
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<RefundPaymentCommand> context)
    {
        var message = context.Message;

        // UPDLOCK: aynı sipariş için eşzamanlı iki iade komutu gelirse ikincisi burada bekler, ilki commit edince
        // ödemeyi Refunded görür. Kilit, endpoint'in outbox transaction'ı boyunca tutulur.
        var payment = await _dbContext.Payments
            .FromSqlInterpolated($"SELECT * FROM Payments WITH (UPDLOCK, ROWLOCK) WHERE OrderId = {message.OrderId}")
            .SingleOrDefaultAsync(context.CancellationToken);

        if (payment is null)
        {
            _logger.LogWarning("[CorrelationId: {CorrelationId}] OrderId: {OrderId} için ödeme kaydı yok, iade edilecek tutar bulunmuyor.",
                message.CorrelationId, message.OrderId);
            return;
        }

        if (payment.Status != PaymentStatus.Success)
        {
            _logger.LogInformation("[CorrelationId: {CorrelationId}] OrderId: {OrderId} ödemesi '{Status}' durumunda, iade yapılmadı.",
                message.CorrelationId, message.OrderId, payment.Status);
            return;
        }

        var (isSuccess, failReason) = _paymentService.RefundPayment(payment.OrderId, payment.PaymentToken, payment.TotalPrice);

        // İade sessizce düşmemeli: hata fırlatılır, mesaj retry ve ardından _error kuyruğuna gider
        if (!isSuccess)
            throw new InvalidOperationException($"OrderId {message.OrderId} için iade başarısız: {failReason}");

        payment.Status = PaymentStatus.Refunded;
        await _dbContext.SaveChangesAsync(context.CancellationToken);

        _logger.LogWarning("[CorrelationId: {CorrelationId}] OrderId: {OrderId} için {Amount} TL iade edildi. Sebep: {Reason}",
            message.CorrelationId, message.OrderId, payment.TotalPrice, message.Reason);
    }
}
