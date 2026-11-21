using EticaretMicroservice.Catalog.Api.Models;
using EticaretMicroservice.Catalog.Api.Services;
using EticaretMicroservice.Shared.Events;
using EticaretMicroservice.Shared.SampleData;
using MassTransit;

namespace EticaretMicroservice.Catalog.Api.Seed;


public class SampleCatalogSeeder : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IBus _bus;
    private readonly ILogger<SampleCatalogSeeder> _logger;

    public SampleCatalogSeeder(IServiceScopeFactory scopeFactory, IBus bus, ILogger<SampleCatalogSeeder> logger)
    {
        _scopeFactory = scopeFactory;
        _bus = bus;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var productService = scope.ServiceProvider.GetRequiredService<IProductService>();

            var existing = await productService.GetAllAsync();
            if (existing.Any(p => p.IsSample))
                return;

            foreach (var sample in SampleCatalog.Products)
            {
                await _bus.Publish(new ProductCreatedEvent
                {
                    ProductId = sample.Id,
                    Name = sample.Name,
                    Price = sample.Price,
                    InitialStock = sample.Stock
                }, stoppingToken);

                await productService.CreateAsync(new Product
                {
                    Id = sample.Id,
                    Name = sample.Name,
                    Description = sample.Description,
                    Price = sample.Price,
                    CompareAtPrice = sample.CompareAtPrice,
                    Category = sample.Category,
                    ImageUrl = sample.ImageUrl,
                    IsSample = true
                });
            }

            _logger.LogInformation("Örnek katalog eklendi: {Count} ürün.", SampleCatalog.Products.Count);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogError(ex, "Örnek katalog eklenemedi.");
        }
    }
}
