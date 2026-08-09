using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EticaretMicroservice.Services.Order.Application.Interfaces
{
    public interface IOrderRepository
    {
        Task<Domain.Entities.Order> AddAsync(Domain.Entities.Order order);
        Task<List<Domain.Entities.Order>> GetOrdersByUserIdAsync(string userId);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default); // 🔹 Eklenen Metod
        Task<Domain.Entities.Order> GetByIdAsync(int id);
    }
}
