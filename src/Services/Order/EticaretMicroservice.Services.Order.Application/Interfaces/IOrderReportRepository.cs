using EticaretMicroservice.Services.Order.Domain.Enums;

namespace EticaretMicroservice.Services.Order.Application.Interfaces;

// Admin sipariş listesi ve raporları: yalnızca okuma. Sorgular SQL tarafında toplanır/sayfalanır; sipariş
// nesneleri belleğe yüklenmez (IOrderRepository aggregate'i yazma tarafı içindir).
public record OrderSearchCriteria(string? Search, OrderStatus? Status, string? BuyerId, int Page, int PageSize);

public record OrderListItemDto(
    int Id, string BuyerId, string ContactName, DateTime CreatedDate, int ItemCount, string PaymentStatus, string OrderStatus, decimal TotalPrice);

// StatusCounts: arama/müşteri filtresi uygulanmış, durum filtresi uygulanmamış sayılar (sekmelerdeki rozetler için)
public record PagedOrdersDto(
    IReadOnlyList<OrderListItemDto> Items, int TotalCount, int Page, int PageSize, IReadOnlyDictionary<string, int> StatusCounts);

public record SalesSummaryDto(
    decimal Revenue, int OrderCount, int CompletedCount, int PendingCount, int CanceledCount,
    decimal AverageOrderValue, double CancelRate, int ItemsSold);

public record DailySalesDto(DateTime Date, decimal Revenue, int Orders);

public record ProductSalesDto(string ProductId, string Name, int Quantity, decimal Revenue);

public record CustomerStatsDto(string BuyerId, int OrderCount, int CompletedCount, decimal TotalSpent, DateTime LastOrderAt, string? City);

// Alıcı: en az bir tamamlanmış siparişi olan müşteri. TotalSpent tamamlanan siparişlerin toplamıdır.
public record CustomerSummaryDto(int BuyerCount, int RepeatBuyerCount, decimal TotalSpent);

public interface IOrderReportRepository
{
    Task<PagedOrdersDto> SearchAsync(OrderSearchCriteria criteria, CancellationToken cancellationToken);

    // Dönemler [fromUtc, toUtc): başlangıç dahil, bitiş hariç. Ciro ve satılan adet yalnızca tamamlanan siparişlerden gelir.
    Task<SalesSummaryDto> GetSummaryAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken);

    // Her gün için bir nokta (boş günler dahil). Gün sınırları fromUtc'den itibaren 24 saatlik dilimlerdir.
    Task<IReadOnlyList<DailySalesDto>> GetDailySalesAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken);

    Task<IReadOnlyList<ProductSalesDto>> GetProductSalesAsync(DateTime fromUtc, DateTime toUtc, int limit, CancellationToken cancellationToken);

    Task<IReadOnlyList<CustomerStatsDto>> GetCustomerStatsAsync(IReadOnlyList<string>? buyerIds, CancellationToken cancellationToken);

    Task<CustomerSummaryDto> GetCustomerSummaryAsync(CancellationToken cancellationToken);
}
