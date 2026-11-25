using EticaretMicroservice.Services.Order.Application.Interfaces;
using EticaretMicroservice.Services.Order.Domain.Entities;
using EticaretMicroservice.Services.Order.Domain.Enums;
using EticaretMicroservice.Services.Order.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EticaretMicroservice.Services.Order.Infrastructure.Repositories;

// Admin liste ve raporları. Hepsi AsNoTracking + projeksiyon/gruplama: sipariş aggregate'leri belleğe yüklenmez,
// toplamlar (SUM/COUNT) ve sayfalama SQL Server'da yapılır.
public class OrderReportRepository : IOrderReportRepository
{
    // Büyük/küçük harf ve aksan duyarsız karşılaştırma: "istanbul" → "İstanbul", "caglar" → "Çağlar", "yilmaz" → "Yılmaz",
    // "isparta" → "Isparta". Kolasyon olmadan (SQL Server varsayılanıyla) bunların çoğu eşleşmez; OrderReportTests bunu sabitler.
    private const string SearchCollation = "Latin1_General_100_CI_AI";

    private const int MinutesPerDay = 24 * 60;

    private readonly OrderDbContext _context;

    public OrderReportRepository(OrderDbContext context)
    {
        _context = context;
    }

    public async Task<PagedOrdersDto> SearchAsync(OrderSearchCriteria criteria, CancellationToken cancellationToken)
    {
        var filtered = _context.Orders.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(criteria.BuyerId))
            filtered = filtered.Where(o => o.BuyerId == criteria.BuyerId);

        // Her terim sipariş no, alıcı adı veya ilden birine uymalı; terimler birbirine "ve" ile bağlanır
        foreach (var term in SplitTerms(criteria.Search))
        {
            var orderNumber = int.TryParse(term.TrimStart('#'), out var id) ? id : -1;
            filtered = filtered.Where(o =>
                o.Id == orderNumber
                || EF.Functions.Collate(o.Address.ContactName, SearchCollation).Contains(term)
                || EF.Functions.Collate(o.Address.City, SearchCollation).Contains(term));
        }

        // Sekme rozetleri: durum filtresi hariç, arama sonucundaki durum dağılımı (tek GROUP BY sorgusu)
        var grouped = await filtered
            .GroupBy(o => o.OrderStatus)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var statusCounts = Enum.GetValues<OrderStatus>()
            .ToDictionary(status => status.ToString(), status => grouped.FirstOrDefault(g => g.Status == status)?.Count ?? 0);

        var totalCount = criteria.Status is { } selected ? statusCounts[selected.ToString()] : statusCounts.Values.Sum();

        if (criteria.Status is { } statusFilter)
            filtered = filtered.Where(o => o.OrderStatus == statusFilter);

        var rows = await filtered
            .OrderByDescending(o => o.CreatedDate)
            .ThenByDescending(o => o.Id)
            .Skip((criteria.Page - 1) * criteria.PageSize)
            .Take(criteria.PageSize)
            .Select(o => new
            {
                o.Id,
                o.BuyerId,
                o.Address.ContactName,
                o.CreatedDate,
                ItemCount = o.OrderItems.Sum(i => i.Quantity),
                TotalPrice = o.OrderItems.Sum(i => i.Price * i.Quantity),
                o.OrderStatus,
                o.PaymentStatus
            })
            .ToListAsync(cancellationToken);

        var items = rows
            .Select(r => new OrderListItemDto(
                r.Id, r.BuyerId, r.ContactName, r.CreatedDate, r.ItemCount, r.PaymentStatus.ToString(), r.OrderStatus.ToString(), r.TotalPrice))
            .ToList();

        return new PagedOrdersDto(items, totalCount, criteria.Page, criteria.PageSize, statusCounts);
    }

    public async Task<SalesSummaryDto> GetSummaryAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        var period = InPeriod(fromUtc, toUtc);

        var counts = await period
            .GroupBy(o => o.OrderStatus)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        int CountOf(OrderStatus status) => counts.FirstOrDefault(c => c.Status == status)?.Count ?? 0;

        // Ciro ve satılan adet yalnızca tamamlanan siparişlerden gelir: iptal edilenlerin ödemesi ya alınmadı ya iade edildi
        var completedItems = period.Where(o => o.OrderStatus == OrderStatus.Tamamlandı).SelectMany(o => o.OrderItems);
        var revenue = await completedItems.SumAsync(i => i.Price * i.Quantity, cancellationToken);
        var itemsSold = await completedItems.SumAsync(i => i.Quantity, cancellationToken);

        var completed = CountOf(OrderStatus.Tamamlandı);
        var canceled = CountOf(OrderStatus.IptalEdildi);
        var decided = completed + canceled;

        return new SalesSummaryDto(
            Revenue: revenue,
            OrderCount: counts.Sum(c => c.Count),
            CompletedCount: completed,
            PendingCount: CountOf(OrderStatus.Beklemede),
            CanceledCount: canceled,
            AverageOrderValue: completed > 0 ? revenue / completed : 0,
            // Sonuçlanmamış (bekleyen) siparişler oranı bozmasın
            CancelRate: decided > 0 ? (double)canceled / decided : 0,
            ItemsSold: itemsSold);
    }

    public async Task<IReadOnlyList<DailySalesDto>> GetDailySalesAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        var period = InPeriod(fromUtc, toUtc);

        // Gün = başlangıçtan geçen tam 24 saatlik dilim sayısı. DATEDIFF(day) kullanılmaz: o UTC gece yarısı sınırlarını sayar,
        // yerel gece yarısından başlayan bir dönemde siparişleri yanlış güne atardı.
        var orderCounts = await period
            .GroupBy(o => EF.Functions.DateDiffMinute(fromUtc, o.CreatedDate) / MinutesPerDay)
            .Select(g => new { Day = g.Key, Orders = g.Count() })
            .ToListAsync(cancellationToken);

        var revenues = await period
            .Where(o => o.OrderStatus == OrderStatus.Tamamlandı)
            .SelectMany(o => o.OrderItems, (o, i) => new
            {
                Day = EF.Functions.DateDiffMinute(fromUtc, o.CreatedDate) / MinutesPerDay,
                Amount = i.Price * i.Quantity
            })
            .GroupBy(x => x.Day)
            .Select(g => new { Day = g.Key, Revenue = g.Sum(x => x.Amount) })
            .ToListAsync(cancellationToken);

        var ordersByDay = orderCounts.ToDictionary(x => x.Day, x => x.Orders);
        var revenueByDay = revenues.ToDictionary(x => x.Day, x => x.Revenue);

        var dayCount = (int)Math.Ceiling((toUtc - fromUtc).TotalDays);
        return Enumerable.Range(0, dayCount)
            .Select(day => new DailySalesDto(
                fromUtc.AddDays(day),
                revenueByDay.GetValueOrDefault(day),
                ordersByDay.GetValueOrDefault(day)))
            .ToList();
    }

    public async Task<IReadOnlyList<ProductSalesDto>> GetProductSalesAsync(DateTime fromUtc, DateTime toUtc, int limit, CancellationToken cancellationToken)
    {
        // Sıralama ve Take SQL'de yapılır; DTO'ya dönüşüm bellekte (kayıt yapıcısıyla yapılan projeksiyon üzerinden OrderBy çevrilemez)
        var rows = await InPeriod(fromUtc, toUtc)
            .Where(o => o.OrderStatus == OrderStatus.Tamamlandı)
            .SelectMany(o => o.OrderItems)
            .GroupBy(i => i.ProductId)
            .Select(g => new
            {
                ProductId = g.Key,
                Name = g.Max(i => i.ProductName),
                Quantity = g.Sum(i => i.Quantity),
                Revenue = g.Sum(i => i.Price * i.Quantity)
            })
            .OrderByDescending(p => p.Revenue)
            .ThenBy(p => p.ProductId)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return rows.Select(p => new ProductSalesDto(p.ProductId, p.Name, p.Quantity, p.Revenue)).ToList();
    }

    public async Task<IReadOnlyList<CustomerStatsDto>> GetCustomerStatsAsync(IReadOnlyList<string>? buyerIds, CancellationToken cancellationToken)
    {
        var orders = _context.Orders.AsNoTracking();
        if (buyerIds is not null)
            orders = orders.Where(o => buyerIds.Contains(o.BuyerId));

        // Harcama tamamlanan siparişlerin kalemlerinden; sipariş sayıları ve son sipariş ayrı bir gruplamadan gelir
        // (SQL, SUM içinde alt sorgu toplamayı kabul etmez)
        var spent = await orders
            .Where(o => o.OrderStatus == OrderStatus.Tamamlandı)
            .SelectMany(o => o.OrderItems, (o, i) => new { o.BuyerId, Amount = i.Price * i.Quantity })
            .GroupBy(x => x.BuyerId)
            .Select(g => new { BuyerId = g.Key, Total = g.Sum(x => x.Amount) })
            .ToDictionaryAsync(x => x.BuyerId, x => x.Total, cancellationToken);

        var buyers = await orders
            .GroupBy(o => o.BuyerId)
            .Select(g => new
            {
                BuyerId = g.Key,
                OrderCount = g.Count(),
                CompletedCount = g.Count(o => o.OrderStatus == OrderStatus.Tamamlandı),
                LastOrderAt = g.Max(o => o.CreatedDate),
                // Şehir, müşterinin en son siparişinin teslimat ilinden alınır
                City = _context.Orders
                    .Where(o => o.BuyerId == g.Key)
                    .OrderByDescending(o => o.CreatedDate)
                    .ThenByDescending(o => o.Id)
                    .Select(o => o.Address.City)
                    .FirstOrDefault()
            })
            .ToListAsync(cancellationToken);

        return buyers
            .Select(b => new CustomerStatsDto(b.BuyerId, b.OrderCount, b.CompletedCount, spent.GetValueOrDefault(b.BuyerId), b.LastOrderAt, b.City))
            .ToList();
    }

    public async Task<CustomerSummaryDto> GetCustomerSummaryAsync(CancellationToken cancellationToken)
    {
        var completed = _context.Orders.AsNoTracking().Where(o => o.OrderStatus == OrderStatus.Tamamlandı);
        var completedPerBuyer = completed.GroupBy(o => o.BuyerId).Select(g => g.Count());

        var buyerCount = await completedPerBuyer.CountAsync(cancellationToken);
        var repeatBuyerCount = await completedPerBuyer.CountAsync(count => count > 1, cancellationToken);
        var totalSpent = await completed.SelectMany(o => o.OrderItems).SumAsync(i => i.Price * i.Quantity, cancellationToken);

        return new CustomerSummaryDto(buyerCount, repeatBuyerCount, totalSpent);
    }

    private IQueryable<Domain.Entities.Order> InPeriod(DateTime fromUtc, DateTime toUtc) =>
        _context.Orders.AsNoTracking().Where(o => o.CreatedDate >= fromUtc && o.CreatedDate < toUtc);

    private static IEnumerable<string> SplitTerms(string? search) =>
        (search ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
}
