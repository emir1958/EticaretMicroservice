using EticaretMicroservice.Basket.Api.Models;

namespace EticaretMicroservice.Basket.Api.Services
{
    public interface IBasketService
    {
        Task<CustomerBasket?> GetBasketAsync(string userId);
        Task<CustomerBasket> UpdateBasketAsync(CustomerBasket basket);
        Task<bool> DeleteBasketAsync(string userId);
    }
}
