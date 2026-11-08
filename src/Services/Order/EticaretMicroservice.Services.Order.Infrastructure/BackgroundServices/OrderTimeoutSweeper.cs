using EticaretMicroservice.Services.Order.Application.Sagas;
using EticaretMicroservice.Services.Order.Infrastructure.Persistence;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EticaretMicroservice.Services.Order.Infrastructure.BackgroundServices;

// Kaçan timeout'lar için emniyet ağı: delayed exchange'deki mesaj kaybolursa (tek node'da tutulur) sipariş beklemede kalmasın.
// Order'a dokunmaz; süresi geçmiş saga instance'larına saganın kendi PaymentTimeoutExpired mesajını gönderir ve kararı saga verir
// (ExpiresAt kontrolü, terminal durumda Ignore). Bu yüzden mükerrer gönderim ve saga ile yarış güvenlidir.
public class OrderTimeoutSweeper : BackgroundService
{
    private const int BatchSize = 100;

    // Saga durum adları State property adlarıdır
    private static readonly string[] PendingStates =
    {
        nameof(OrderStateMachine.AwaitingStock),
        nameof(OrderStateMachine.AwaitingPayment)
    };

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IBus _bus;
    private readonly OrderSagaOptions _options;
    private readonly ILogger<OrderTimeoutSweeper> _logger;

    public OrderTimeoutSweeper(IServiceScopeFactory scopeFactory, IBus bus, IOptions<OrderSagaOptions> options, ILogger<OrderTimeoutSweeper> logger)
    {
        _scopeFactory = scopeFactory;
        _bus = bus;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OrderTimeoutSweeper devreye girdi. Aralık: {Interval}, tolerans: {Grace}",
            _options.SweeperInterval, _options.SweeperGrace);

        using var timer = new PeriodicTimer(_options.SweeperInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await SweepAsync(DateTime.UtcNow, stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogError(ex, "Süresi geçmiş saga instance'ları taranırken hata oluştu.");
            }
        }
    }

    public async Task<int> SweepAsync(DateTime utcNow, CancellationToken cancellationToken)
    {
        var overdue = await FindOverdueAsync(utcNow, cancellationToken);
        await DispatchAsync(overdue, cancellationToken);
        return overdue.Count;
    }

    // Yalnızca saga instance'ı olan siparişler hedeflenir: instance'ı olmayan eski siparişler için mesaj fault üretirdi.
    // ExpiresAt'i olmayan (Step 4 öncesi oluşmuş) instance'larda son tarih CreatedAt + PaymentTimeout kabul edilir.
    public async Task<IReadOnlyList<Guid>> FindOverdueAsync(DateTime utcNow, CancellationToken cancellationToken)
    {
        var expiredBefore = utcNow - _options.SweeperGrace;
        var createdBefore = expiredBefore - _options.PaymentTimeout;

        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

        return await dbContext.Set<OrderState>().AsNoTracking()
            .Where(s => PendingStates.Contains(s.CurrentState)
                && (s.ExpiresAt < expiredBefore || (s.ExpiresAt == null && s.CreatedAt < createdBefore)))
            .OrderBy(s => s.ExpiresAt)
            .Select(s => s.CorrelationId)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);
    }

    // Outbox kullanılmaz: mesajla aynı transaction'da yazılacak bir DB değişikliği yok. Gönderim kaybolursa instance
    // beklemede kalır ve bir sonraki turda yeniden seçilir.
    public async Task DispatchAsync(IReadOnlyCollection<Guid> correlationIds, CancellationToken cancellationToken)
    {
        foreach (var correlationId in correlationIds)
            await _bus.Publish(new PaymentTimeoutExpired { CorrelationId = correlationId }, cancellationToken);

        if (correlationIds.Count > 0)
            _logger.LogWarning("Zamanlanmış timeout'u gelmemiş {Count} saga instance'ına PaymentTimeoutExpired gönderildi.", correlationIds.Count);
    }
}
