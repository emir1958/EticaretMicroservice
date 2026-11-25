using EticaretMicroservice.Services.Order.Application.Interfaces;
using MediatR;

namespace EticaretMicroservice.Services.Order.Application.Queries;

// Status: OrderStatus enum adı ("Beklemede", "Tamamlandı", "IptalEdildi"); boşsa filtre yok.
// Tarihler DateTimeOffset: istemcinin yerel gün başlangıcı gibi kesin bir an olarak gelir, saat dilimi kayması olmaz.
public record SearchOrdersQuery(string? Search, string? Status, string? BuyerId, int Page, int PageSize) : IRequest<PagedOrdersDto>;

public record GetSalesSummaryQuery(DateTimeOffset From, DateTimeOffset To) : IRequest<SalesSummaryDto>;

public record GetDailySalesQuery(DateTimeOffset From, DateTimeOffset To) : IRequest<IReadOnlyList<DailySalesDto>>;

public record GetProductSalesQuery(DateTimeOffset From, DateTimeOffset To, int Limit) : IRequest<IReadOnlyList<ProductSalesDto>>;

// BuyerIds boşsa tüm alıcılar döner
public record GetCustomerStatsQuery(IReadOnlyList<string>? BuyerIds = null) : IRequest<IReadOnlyList<CustomerStatsDto>>;

public record GetCustomerSummaryQuery : IRequest<CustomerSummaryDto>;
