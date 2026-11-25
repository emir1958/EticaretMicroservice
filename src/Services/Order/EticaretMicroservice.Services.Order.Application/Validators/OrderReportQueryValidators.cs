using EticaretMicroservice.Services.Order.Application.Queries;
using EticaretMicroservice.Services.Order.Domain.Enums;
using FluentValidation;

namespace EticaretMicroservice.Services.Order.Application.Validators;

public class SearchOrdersQueryValidator : AbstractValidator<SearchOrdersQuery>
{
    public const int MaxPageSize = 100;

    public SearchOrdersQueryValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1).WithMessage("Sayfa numarası en az 1 olmalıdır.");
        RuleFor(x => x.PageSize).InclusiveBetween(1, MaxPageSize).WithMessage($"Sayfa boyutu 1 ile {MaxPageSize} arasında olmalıdır.");
        RuleFor(x => x.Search).MaximumLength(100).WithMessage("Arama metni en fazla 100 karakter olabilir.");

        RuleFor(x => x.Status)
            .Must(status => Enum.TryParse<OrderStatus>(status, out var parsed) && Enum.IsDefined(parsed))
            .When(x => !string.IsNullOrEmpty(x.Status))
            .WithMessage("Geçersiz sipariş durumu.");
    }
}

// Rapor dönemleri: sınırsız aralık, günlük seride çok sayıda nokta üretir; makul bir üst sınır konur
public static class ReportPeriod
{
    public static readonly TimeSpan MaxLength = TimeSpan.FromDays(400);
}

public class GetSalesSummaryQueryValidator : AbstractValidator<GetSalesSummaryQuery>
{
    public GetSalesSummaryQueryValidator()
    {
        RuleFor(x => x.To).GreaterThan(x => x.From).WithMessage("Bitiş tarihi başlangıçtan sonra olmalıdır.");
        RuleFor(x => x).Must(x => x.To - x.From <= ReportPeriod.MaxLength).WithMessage("Rapor dönemi en fazla 400 gün olabilir.");
    }
}

public class GetDailySalesQueryValidator : AbstractValidator<GetDailySalesQuery>
{
    public GetDailySalesQueryValidator()
    {
        RuleFor(x => x.To).GreaterThan(x => x.From).WithMessage("Bitiş tarihi başlangıçtan sonra olmalıdır.");
        RuleFor(x => x).Must(x => x.To - x.From <= ReportPeriod.MaxLength).WithMessage("Rapor dönemi en fazla 400 gün olabilir.");
    }
}

public class GetProductSalesQueryValidator : AbstractValidator<GetProductSalesQuery>
{
    public GetProductSalesQueryValidator()
    {
        RuleFor(x => x.To).GreaterThan(x => x.From).WithMessage("Bitiş tarihi başlangıçtan sonra olmalıdır.");
        RuleFor(x => x).Must(x => x.To - x.From <= ReportPeriod.MaxLength).WithMessage("Rapor dönemi en fazla 400 gün olabilir.");
        RuleFor(x => x.Limit).InclusiveBetween(1, 1000).WithMessage("Limit 1 ile 1000 arasında olmalıdır.");
    }
}

public class GetCustomerStatsQueryValidator : AbstractValidator<GetCustomerStatsQuery>
{
    public const int MaxBuyerIds = 100;

    public GetCustomerStatsQueryValidator()
    {
        RuleFor(x => x.BuyerIds!.Count)
            .LessThanOrEqualTo(MaxBuyerIds)
            .When(x => x.BuyerIds is not null)
            .WithMessage($"Tek istekte en fazla {MaxBuyerIds} müşteri sorgulanabilir.");
    }
}
