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
                // Uzunluklar OrderDbContext'teki kolonlarla aynı: aşan değer SQL'de 500 yerine burada 400 döner
                addr.RuleFor(a => a.ContactName).NotEmpty().WithMessage("Alıcı adı soyadı zorunludur.")
                    .MaximumLength(100).WithMessage("Alıcı adı soyadı en fazla 100 karakter olabilir.");
                addr.RuleFor(a => a.Phone).Matches(@"^0?5\d{9}$").WithMessage("Telefon numarası 05XX XXX XX XX biçiminde olmalıdır.")
                    .When(a => !string.IsNullOrEmpty(a.Phone));
                addr.RuleFor(a => a.City).NotEmpty().WithMessage("İl alanı zorunludur.")
                    .MaximumLength(50).WithMessage("İl en fazla 50 karakter olabilir.");
                addr.RuleFor(a => a.District).NotEmpty().WithMessage("İlçe alanı zorunludur.")
                    .MaximumLength(50).WithMessage("İlçe en fazla 50 karakter olabilir.");
                addr.RuleFor(a => a.Street).NotEmpty().WithMessage("Sokak/Cadde alanı zorunludur.")
                    .MaximumLength(100).WithMessage("Sokak/Cadde en fazla 100 karakter olabilir.");
                addr.RuleFor(a => a.ZipCode).Matches(@"^\d{5}$").WithMessage("Posta kodu 5 haneli olmalıdır.")
                    .When(a => !string.IsNullOrEmpty(a.ZipCode));
                addr.RuleFor(a => a.Line).NotEmpty().WithMessage("Açık adres satırı zorunludur.")
                    .MaximumLength(250).WithMessage("Açık adres en fazla 250 karakter olabilir.");
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