using EticaretMicroservice.Identity.Api.Dtos;
using FluentValidation;

namespace EticaretMicroservice.Identity.Api.Validators;

public class RegisterDtoValidator : AbstractValidator<RegisterDto>
{
    public RegisterDtoValidator()
    {
        // Uzunluklar AppIdentityDbContext'teki kolonlarla aynı: aşan değer SQL'de hata yerine burada 400 döner
        RuleFor(x => x.Username)
            .NotEmpty().WithMessage("Kullanıcı adı zorunludur.")
            .MaximumLength(100).WithMessage("Kullanıcı adı en fazla 100 karakter olabilir.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("E-posta zorunludur.")
            .EmailAddress().WithMessage("Geçerli bir e-posta adresi girin.")
            .MaximumLength(150).WithMessage("E-posta en fazla 150 karakter olabilir.");

        // BCrypt 72 baytın ötesini sessizce yok sayar; üst sınır bu yüzden var
        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Şifre zorunludur.")
            .MinimumLength(8).WithMessage("Şifre en az 8 karakter olmalıdır.")
            .MaximumLength(72).WithMessage("Şifre en fazla 72 karakter olabilir.");
    }
}
