using EticaretMicroservice.Catalog.Api.Models;

namespace EticaretMicroservice.Catalog.Api.Services
{
    public interface IProductService
    {
        Task<IEnumerable<Product>> GetAllAsync();
        Task<Product?> GetByIdAsync(string id);
        Task<Product> CreateAsync(Product product);
        Task<bool> DeleteAsync(string id);
    }
}
