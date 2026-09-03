using System.Net.Http.Json;
using EticaretMicroservice.Services.Order.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace EticaretMicroservice.Services.Order.Infrastructure.Repositories;

public class CatalogRepository : ICatalogRepository
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CatalogRepository> _logger;

    public CatalogRepository(HttpClient httpClient, ILogger<CatalogRepository> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CatalogProductDto?> GetProductByIdAsync(string productId, CancellationToken cancellationToken = default)
    {
        try
        {
            // Catalog API'nin GET /api/products/{id} endpoint'ini çağırır
            var response = await _httpClient.GetAsync($"/api/products/{productId}", cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Catalog API ürün bulamadı veya hata döndü. ProductId: {ProductId}, StatusCode: {Status}",
                    productId, response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<CatalogProductDto>(cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Catalog API ile iletişim kurulurken hata oluştu. ProductId: {ProductId}", productId);
            throw new InvalidOperationException("Katalog servisiyle iletişim kurulamadı.", ex);
        }
    }
}