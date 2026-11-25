using EticaretMicroservice.Identity.Api.Dtos;
using EticaretMicroservice.Identity.Api.Validators;
using FluentAssertions;

namespace EticaretMicroservice.UnitTests.Identity;

public class AuthValidatorTests
{
    private readonly RegisterDtoValidator _register = new();
    private readonly LoginDtoValidator _login = new();

    private static RegisterDto ValidRegister() => new() { Username = "emir", Email = "emir@test.com", Password = "Abcdef12" };

    [Fact]
    public void Register_WhenAllFieldsAreValid_ShouldPass()
    {
        _register.Validate(ValidRegister()).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Register_WhenBodyIsEmpty_ShouldFail()
    {
        var result = _register.Validate(new RegisterDto());

        result.Errors.Select(e => e.PropertyName).Should().Contain(new[] { "Username", "Email", "Password" });
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("a@")]
    [InlineData("")]
    public void Register_WhenEmailIsInvalid_ShouldFail(string email)
    {
        var dto = ValidRegister();
        dto.Email = email;

        _register.Validate(dto).Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("1234567")]
    public void Register_WhenPasswordIsShorterThan8_ShouldFail(string password)
    {
        var dto = ValidRegister();
        dto.Password = password;

        _register.Validate(dto).Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void Register_WhenPasswordExceeds72Characters_ShouldFail()
    {
        var dto = ValidRegister();
        dto.Password = new string('a', 73);

        _register.Validate(dto).Errors.Should().Contain(e => e.PropertyName == "Password");
    }

    [Fact]
    public void Register_WhenValuesExceedColumnLengths_ShouldFail()
    {
        var dto = ValidRegister();
        dto.Username = new string('u', 101);
        dto.Email = new string('e', 150) + "@t.com";

        var result = _register.Validate(dto);

        result.Errors.Should().Contain(e => e.PropertyName == "Username");
        result.Errors.Should().Contain(e => e.PropertyName == "Email");
    }

    [Fact]
    public void Login_WhenFieldsAreEmpty_ShouldFail()
    {
        var result = _login.Validate(new LoginDto());

        result.Errors.Select(e => e.PropertyName).Should().Contain(new[] { "Email", "Password" });
    }

    [Fact]
    public void Login_WhenPasswordIsShort_ShouldPass()
    {
        // Uzunluk kuralı yalnızca kayıtta: kural öncesi kısa şifreyle açılmış hesaplar giriş yapabilmeli
        var dto = new LoginDto { Email = "old@test.com", Password = "abc" };

        _login.Validate(dto).IsValid.Should().BeTrue();
    }
}
