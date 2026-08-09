using EticaretMicroservice.Catalog.Api.Models;
using EticaretMicroservice.Catalog.Api.Settings;
using MongoDB.Driver;

namespace EticaretMicroservice.Catalog.Api.Services
{
    public class ProductService : IProductService
    {
        private readonly IMongoCollection<Product> _productCollection;

        public ProductService(IDatabaseSettings databaseSettings)
        {
            var client = new MongoClient(databaseSettings.ConnectionString);
            var database = client.GetDatabase(databaseSettings.DatabaseName);
            _productCollection = database.GetCollection<Product>(databaseSettings.ProductsCollectionName);
        }

        public async Task<IEnumerable<Product>> GetAllAsync()
        {
            return await _productCollection.Find(product => true).ToListAsync();
        }

        public async Task<Product?> GetByIdAsync(string id)
        {
            return await _productCollection.Find(p => p.Id == id).FirstOrDefaultAsync();
        }

        public async Task<Product> CreateAsync(Product product)
        {
            await _productCollection.InsertOneAsync(product);
            return product;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var result = await _productCollection.DeleteOneAsync(p => p.Id == id);
            return result.DeletedCount > 0;
        }
    }
}