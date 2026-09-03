using EticaretMicroservice.Shared.Events;
using EticaretMicroservice.Stock.Api.Services;
using MassTransit;

namespace EticaretMicroservice.Stock.Api.Consumers;

public class ProductCreatedEventConsumer : IConsumer<ProductCreatedEvent>
{
    private readonly IStockService _stockService;
    private readonly ILogger<ProductCreatedEventConsumer> _logger;

    public ProductCreatedEventConsumer(IStockService stockService, ILogger<ProductCreatedEventConsumer> logger)
    {
        _stockService = stockService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<ProductCreatedEvent> context)
    {
        var message = context.Message;
        _logger.LogInformation("Stock.Api: Yeni ürün oluşturuldu event'i alındı. ProductId: {ProductId}, Başlangıç Stoğu: {Stock}",
            message.ProductId, message.InitialStock);

        await _stockService.CreateStockAsync(message.ProductId, message.InitialStock);
    }
}