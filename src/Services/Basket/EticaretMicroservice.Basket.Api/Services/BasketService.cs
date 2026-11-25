using System.Text.Json;
using EticaretMicroservice.Basket.Api.Models;
using Microsoft.Extensions.Caching.Distributed;

namespace EticaretMicroservice.Basket.Api.Services;

public class BasketService : IBasketService
{
    private readonly IDistributedCache _redisCache;

    public BasketService(IDistributedCache redisCache)
    {
        _redisCache = redisCache;
    }

    public async Task<CustomerBasket?> GetBasketAsync(string userId)
    {
        var basketJson = await _redisCache.GetStringAsync(userId);
        if (string.IsNullOrEmpty(basketJson))
            return null;

        return JsonSerializer.Deserialize<CustomerBasket>(basketJson);
    }

    public async Task<CustomerBasket> UpdateBasketAsync(CustomerBasket basket)
    {
        var basketJson = JsonSerializer.Serialize(basket);

        // Terk edilen sepetler Redis belleğinde süresiz kalmasın
        var options = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30)
        };

        await _redisCache.SetStringAsync(basket.UserId, basketJson, options);

        return basket;
    }

    public async Task<bool> DeleteBasketAsync(string userId)
    {
        await _redisCache.RemoveAsync(userId);
        return true;
    }
}