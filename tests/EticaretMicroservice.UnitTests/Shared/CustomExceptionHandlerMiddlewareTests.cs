using System.Text.Json;
using EticaretMicroservice.Shared.Exceptions;
using EticaretMicroservice.Shared.Middlewares;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;

namespace EticaretMicroservice.UnitTests.Shared;

public class CustomExceptionHandlerMiddlewareTests
{
    [Fact]
    public async Task UnexpectedInvalidOperationException_Returns500WithoutLeakingMessage()
    {
        var (status, body) = await InvokeWith(new InvalidOperationException("Server=sqlserver;Password=secret"));

        status.Should().Be(StatusCodes.Status500InternalServerError);
        body.GetProperty("detail").GetString().Should().NotContain("secret");
    }

    [Fact]
    public async Task BusinessRuleException_Returns422WithMessage()
    {
        var (status, body) = await InvokeWith(new BusinessRuleException("Geçersiz ürün."));

        status.Should().Be(StatusCodes.Status422UnprocessableEntity);
        body.GetProperty("detail").GetString().Should().Be("Geçersiz ürün.");
    }

    [Fact]
    public async Task ServiceUnavailableException_Returns503WithMessage()
    {
        var (status, body) = await InvokeWith(new ServiceUnavailableException("Katalog servisine ulaşılamıyor."));

        status.Should().Be(StatusCodes.Status503ServiceUnavailable);
        body.GetProperty("detail").GetString().Should().Be("Katalog servisine ulaşılamıyor.");
    }

    [Fact]
    public async Task ValidationException_Returns400WithFieldErrors()
    {
        var exception = new ValidationException(new[] { new ValidationFailure("Address.City", "İl alanı zorunludur.") });

        var (status, body) = await InvokeWith(exception);

        status.Should().Be(StatusCodes.Status400BadRequest);
        body.GetProperty("errors").GetProperty("Address.City")[0].GetString().Should().Be("İl alanı zorunludur.");
    }

    private static async Task<(int Status, JsonElement Body)> InvokeWith(Exception exception)
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new CustomExceptionHandlerMiddleware(
            _ => throw exception,
            NullLogger<CustomExceptionHandlerMiddleware>.Instance);

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        var body = await JsonSerializer.DeserializeAsync<JsonElement>(context.Response.Body);
        return (context.Response.StatusCode, body);
    }
}
