using System.Net;
using System.Net.Http.Json;
using EticaretMicroservice.Services.Order.Application.Interfaces;
using EticaretMicroservice.Shared.Exceptions;
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
        HttpResponseMessage response;
        try
        {
            response = await _httpClient.GetAsync($"/api/products/{productId}", cancellationToken);
        }
        catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
        {
            // Timeout, açık circuit breaker veya bağlantı hatası: retry'lar (Polly) tükenmiştir
            _logger.LogError(ex, "Catalog API ile iletişim kurulamadı. ProductId: {ProductId}", productId);
            throw new ServiceUnavailableException("Katalog servisine şu anda ulaşılamıyor. Lütfen biraz sonra tekrar deneyin.", ex);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Catalog API ürünü bulamadı. ProductId: {ProductId}", productId);
            return null;
        }

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("Catalog API hata döndü. ProductId: {ProductId}, StatusCode: {Status}", productId, response.StatusCode);
            throw new ServiceUnavailableException("Katalog servisine şu anda ulaşılamıyor. Lütfen biraz sonra tekrar deneyin.");
        }

        return await response.Content.ReadFromJsonAsync<CatalogProductDto>(cancellationToken: cancellationToken);
    }
}