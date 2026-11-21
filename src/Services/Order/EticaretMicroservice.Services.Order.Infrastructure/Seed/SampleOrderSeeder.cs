using EticaretMicroservice.Services.Order.Domain.ValueObjects;
using EticaretMicroservice.Services.Order.Infrastructure.Persistence;
using EticaretMicroservice.Shared.SampleData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EticaretMicroservice.Services.Order.Infrastructure.Seed;

// Yalnızca Seed:SampleData açıkken kaydedilir. Yönetim panelindeki raporların boş kalmaması için son 60 güne
// yayılmış, sonuçlanmış örnek siparişler ekler. Bir kez çalışır ve yalnızca örnek müşterilere (SampleCustomers) sipariş yazar.
//
// Bu siparişler saga'dan geçmez: Stock ve Payment'ta karşılıkları yoktur, stok düşmez. Durum geçişleri yine
// aggregate metotlarıyla yapılır; böylece sipariş geçmişi gerçek siparişlerdekiyle aynı kurallarla oluşur.
public class SampleOrderSeeder : IHostedService
{
    private const int DaysBack = 60;
    private const int OrderCount = 84;

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SampleOrderSeeder> _logger;

    public SampleOrderSeeder(IServiceScopeFactory scopeFactory, ILogger<SampleOrderSeeder> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await SeedAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Örnek veri eklenemezse servis yine de ayağa kalkar
            _logger.LogError(ex, "Örnek siparişler eklenemedi.");
        }
    }

    private async Task SeedAsync(CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

        var sampleBuyerIds = SampleCustomers.Customers.Select(c => c.Id).ToList();
        if (await dbContext.Orders.AnyAsync(o => sampleBuyerIds.Contains(o.BuyerId), cancellationToken))
            return;

        // Sabit tohum: her ortamda aynı dağılım oluşur
        var random = new Random(20261115);
        var now = DateTime.UtcNow;

        var createdDates = Enumerable.Range(0, OrderCount)
            .Select(_ => RandomOrderDate(random, now))
            .OrderBy(date => date)
            .ToList();

        foreach (var createdAt in createdDates)
        {
            var customer = SampleCustomers.Customers[random.Next(SampleCustomers.Customers.Count)];
            var order = new Domain.Entities.Order(customer.Id, BuildAddress(customer, random), Guid.NewGuid());

            var itemCount = random.Next(100) < 55 ? 1 : random.Next(2, 4);
            foreach (var product in SampleCatalog.Products.OrderBy(_ => random.Next()).Take(itemCount))
            {
                order.AddOrderItem(product.Id, product.Name, product.Price, random.Next(100) < 80 ? 1 : 2);
            }

            dbContext.Orders.Add(order);

            // Kurucu sipariş tarihini "şimdi" yazar; geçmişe taşımak EF üzerinden yapılır, domain modeline seed'e özel kapı açılmaz
            dbContext.Entry(order).Property(o => o.CreatedDate).CurrentValue = createdAt;
            dbContext.Entry(order.History.First()).Property(h => h.OccurredAt).CurrentValue = createdAt;

            ApplyOutcome(order, createdAt, random.Next(100));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Örnek siparişler eklendi: {Count} adet.", OrderCount);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static void ApplyOutcome(Domain.Entities.Order order, DateTime createdAt, int roll)
    {
        var stockAt = createdAt.AddSeconds(2);
        var paymentAt = createdAt.AddSeconds(5);

        if (roll < 82)
        {
            order.TryMarkStockReserved(stockAt);
            order.TryMarkPaymentReceived(paymentAt);
            order.TrySetStatusToCompleted(paymentAt);
        }
        else if (roll < 91)
        {
            order.TryMarkStockReserved(stockAt);
            order.TryMarkPaymentFailed(paymentAt, "Kart limiti yetersiz.");
            order.TrySetStatusToCanceled(paymentAt, "Ödeme alınamadı.");
        }
        else if (roll < 96)
        {
            order.TryMarkStockFailed(stockAt, "Yetersiz stok.");
            order.TrySetStatusToCanceled(stockAt, "Stok yetersiz olduğu için sipariş iptal edildi.");
        }
        else
        {
            // Ödeme süresi dolduktan sonra gelen ödeme: sipariş iptal kalır, ödeme iade edilir
            var canceledAt = createdAt.AddMinutes(15);
            order.TryMarkStockReserved(stockAt);
            order.TrySetStatusToCanceled(canceledAt, "Ödeme süresi doldu.");
            order.TryMarkPaymentReceived(canceledAt.AddSeconds(40));
            order.TryMarkRefunded(canceledAt.AddSeconds(45));
        }
    }

    // Son günlere doğru yoğunlaşan, gündüz saatlerine yayılan tarihler
    private static DateTime RandomOrderDate(Random random, DateTime now)
    {
        var daysAgo = (int)(Math.Pow(random.NextDouble(), 1.4) * DaysBack) + 1;
        var day = now.Date.AddDays(-daysAgo);
        return day.AddHours(random.Next(6, 21)).AddMinutes(random.Next(60)).AddSeconds(random.Next(60));
    }

    private static Address BuildAddress(SampleCustomer customer, Random random)
    {
        string[] streets = { "Bağdat Caddesi", "Atatürk Bulvarı", "Cumhuriyet Caddesi", "İnönü Sokak", "Gül Sokak", "Lale Caddesi" };

        return new Address(
            customer.Username,
            $"05{random.Next(30, 60)}{random.Next(1000000, 9999999)}",
            customer.City,
            customer.District,
            streets[random.Next(streets.Length)],
            random.Next(10000, 81999).ToString(),
            $"No: {random.Next(1, 120)} Daire: {random.Next(1, 24)}");
    }
}
