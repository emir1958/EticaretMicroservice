using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EticaretMicroservice.Services.Order.Application.Interfaces
{
    public record CatalogProductDto(string Id, string Name, string Description, decimal Price);

    public interface ICatalogRepository
    {
        Task<CatalogProductDto?> GetProductByIdAsync(string productId, CancellationToken cancellationToken = default);
    }
}
