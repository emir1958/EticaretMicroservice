using EticaretMicroservice.Services.Order.Application.Commands;
using EticaretMicroservice.Services.Order.Application.Dtos;
using EticaretMicroservice.Services.Order.Application.Validators;
using FluentAssertions;
using Xunit;

namespace EticaretMicroservice.UnitTests.Order;

public class CreateOrderCommandValidatorTests
{
    private readonly CreateOrderCommandValidator _validator = new();

    private static CreateOrderCommand CreateCommand(Action<AddressDto>? changeAddress = null)
    {
        var address = new AddressDto
        {
            ContactName = "Ayşe Yılmaz",
            Phone = "05321234567",
            City = "İstanbul",
            District = "Kadıköy",
            Street = "Moda Cad.",
            ZipCode = "34710",
            Line = "No:1 Daire:5"
        };
        changeAddress?.Invoke(address);

        return new CreateOrderCommand
        {
            BuyerId = "buyer-123",
            Address = address,
            Payment = new PaymentDto { PaymentToken = "tok_test" },
            OrderItems = new List<OrderItemDto> { new() { ProductId = "p1", ProductName = "Mouse", Price = 100m, Quantity = 1 } }
        };
    }

    private List<string> InvalidProperties(CreateOrderCommand command) =>
        _validator.Validate(command).Errors.Select(e => e.PropertyName).ToList();

    [Fact]
    public void Validate_WhenAddressIsComplete_ShouldPass()
    {
        _validator.Validate(CreateCommand()).IsValid.Should().BeTrue();
    }

    // Telefon ve posta kodu opsiyonel
    [Fact]
    public void Validate_WhenOptionalFieldsAreEmpty_ShouldPass()
    {
        var command = CreateCommand(a =>
        {
            a.Phone = "";
            a.ZipCode = "";
        });

        _validator.Validate(command).IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WhenContactNameIsEmpty_ShouldFail()
    {
        InvalidProperties(CreateCommand(a => a.ContactName = "")).Should().Contain("Address.ContactName");
    }

    // Kolon uzunluğunu aşan değer SQL'e ulaşmadan reddedilmeli (aksi halde 500)
    [Fact]
    public void Validate_WhenCityExceedsColumnLength_ShouldFail()
    {
        InvalidProperties(CreateCommand(a => a.City = new string('a', 51))).Should().Contain("Address.City");
    }

    [Theory]
    [InlineData("3471")]
    [InlineData("347100")]
    [InlineData("34A10")]
    public void Validate_WhenZipCodeIsNotFiveDigits_ShouldFail(string zipCode)
    {
        InvalidProperties(CreateCommand(a => a.ZipCode = zipCode)).Should().Contain("Address.ZipCode");
    }

    [Theory]
    [InlineData("5321234567")]
    [InlineData("05321234567")]
    public void Validate_WhenPhoneIsMobileNumber_ShouldPass(string phone)
    {
        _validator.Validate(CreateCommand(a => a.Phone = phone)).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("0532 123 45 67")]
    [InlineData("02161234567")]
    [InlineData("053212345")]
    public void Validate_WhenPhoneIsInvalid_ShouldFail(string phone)
    {
        InvalidProperties(CreateCommand(a => a.Phone = phone)).Should().Contain("Address.Phone");
    }
}
