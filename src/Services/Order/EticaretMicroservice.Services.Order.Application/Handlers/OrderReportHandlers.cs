using EticaretMicroservice.Services.Order.Application.Interfaces;
using EticaretMicroservice.Services.Order.Application.Queries;
using EticaretMicroservice.Services.Order.Domain.Enums;
using MediatR;

namespace EticaretMicroservice.Services.Order.Application.Handlers;

public class SearchOrdersQueryHandler : IRequestHandler<SearchOrdersQuery, PagedOrdersDto>
{
    private readonly IOrderReportRepository _reports;

    public SearchOrdersQueryHandler(IOrderReportRepository reports) => _reports = reports;

    public Task<PagedOrdersDto> Handle(SearchOrdersQuery request, CancellationToken cancellationToken)
    {
        // Durum adının geçerliliği SearchOrdersQueryValidator'da denetlenir
        OrderStatus? status = Enum.TryParse<OrderStatus>(request.Status, out var parsed) ? parsed : null;

        var criteria = new OrderSearchCriteria(request.Search, status, request.BuyerId, request.Page, request.PageSize);
        return _reports.SearchAsync(criteria, cancellationToken);
    }
}

public class GetSalesSummaryQueryHandler : IRequestHandler<GetSalesSummaryQuery, SalesSummaryDto>
{
    private readonly IOrderReportRepository _reports;

    public GetSalesSummaryQueryHandler(IOrderReportRepository reports) => _reports = reports;

    public Task<SalesSummaryDto> Handle(GetSalesSummaryQuery request, CancellationToken cancellationToken) =>
        _reports.GetSummaryAsync(request.From.UtcDateTime, request.To.UtcDateTime, cancellationToken);
}

public class GetDailySalesQueryHandler : IRequestHandler<GetDailySalesQuery, IReadOnlyList<DailySalesDto>>
{
    private readonly IOrderReportRepository _reports;

    public GetDailySalesQueryHandler(IOrderReportRepository reports) => _reports = reports;

    public Task<IReadOnlyList<DailySalesDto>> Handle(GetDailySalesQuery request, CancellationToken cancellationToken) =>
        _reports.GetDailySalesAsync(request.From.UtcDateTime, request.To.UtcDateTime, cancellationToken);
}

public class GetProductSalesQueryHandler : IRequestHandler<GetProductSalesQuery, IReadOnlyList<ProductSalesDto>>
{
    private readonly IOrderReportRepository _reports;

    public GetProductSalesQueryHandler(IOrderReportRepository reports) => _reports = reports;

    public Task<IReadOnlyList<ProductSalesDto>> Handle(GetProductSalesQuery request, CancellationToken cancellationToken) =>
        _reports.GetProductSalesAsync(request.From.UtcDateTime, request.To.UtcDateTime, request.Limit, cancellationToken);
}

public class GetCustomerStatsQueryHandler : IRequestHandler<GetCustomerStatsQuery, IReadOnlyList<CustomerStatsDto>>
{
    private readonly IOrderReportRepository _reports;

    public GetCustomerStatsQueryHandler(IOrderReportRepository reports) => _reports = reports;

    public Task<IReadOnlyList<CustomerStatsDto>> Handle(GetCustomerStatsQuery request, CancellationToken cancellationToken) =>
        _reports.GetCustomerStatsAsync(request.BuyerIds, cancellationToken);
}

public class GetCustomerSummaryQueryHandler : IRequestHandler<GetCustomerSummaryQuery, CustomerSummaryDto>
{
    private readonly IOrderReportRepository _reports;

    public GetCustomerSummaryQueryHandler(IOrderReportRepository reports) => _reports = reports;

    public Task<CustomerSummaryDto> Handle(GetCustomerSummaryQuery request, CancellationToken cancellationToken) =>
        _reports.GetCustomerSummaryAsync(cancellationToken);
}
