using EticaretMicroservice.Services.Order.Application.Commands;
using FluentValidation;

namespace EticaretMicroservice.Services.Order.Application.Validators;

public class CreateOrderCommandValidator : AbstractValidator<CreateOrderCommand>
{
    public CreateOrderCommandValidator()
    {
        RuleFor(x => x.BuyerId)
            .NotEmpty().WithMessage("Alıcı kimliği (BuyerId) boş olamaz.");

        RuleFor(x => x.Payment)
            .NotNull().WithMessage("Ödeme bilgileri zorunludur.")
            .ChildRules(payment =>
            {
                payment.RuleFor(p => p.PaymentToken)
                    .NotEmpty().WithMessage("Ödeme tokenı boş olamaz.");
            });

        RuleFor(x => x.Address)
            .NotNull().WithMessage("Teslimat adresi zorunludur.")
            .ChildRules(addr =>
            {
                addr.RuleFor(a => a.City).NotEmpty().WithMessage("İl alanı zorunludur.");
                addr.RuleFor(a => a.District).NotEmpty().WithMessage("İlçe alanı zorunludur.");
                addr.RuleFor(a => a.Street).NotEmpty().WithMessage("Sokak/Cadde alanı zorunludur.");
                addr.RuleFor(a => a.Line).NotEmpty().WithMessage("Açık adres satırı zorunludur.");
            });

        RuleFor(x => x.OrderItems)
            .NotEmpty().WithMessage("Sipariş en az bir ürün içermelidir.")
            .Must(items => items != null && items.Count > 0).WithMessage("Sepet boş olamaz.");

        RuleForEach(x => x.OrderItems).ChildRules(item =>
        {
            item.RuleFor(i => i.ProductId)
                .NotEmpty().WithMessage("Ürün Id boş olamaz.");

            item.RuleFor(i => i.Quantity)
                .GreaterThan(0).WithMessage("Ürün adedi en az 1 olmalıdır.");

            item.RuleFor(i => i.Price)
                .GreaterThan(0).WithMessage("Ürün fiyatı 0'dan büyük olmalıdır.");
        });
    }
}