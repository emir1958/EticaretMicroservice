using EticaretMicroservice.Services.Order.Application.Interfaces;
using EticaretMicroservice.Services.Order.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EticaretMicroservice.Services.Order.Infrastructure.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly OrderDbContext _context;

        public OrderRepository(OrderDbContext context)
        {
            _context = context;
        }

        public async Task<Domain.Entities.Order> AddAsync(Domain.Entities.Order order)
        {
            await _context.Orders.AddAsync(order);
            return order;
        }

        public async Task<List<Domain.Entities.Order>> GetOrdersByUserIdAsync(string userId)
        {
            // Eager Loading: Siparişleri çekerken sipariş kalemlerini (OrderItems) de dahil ediyoruz (.Include)
            return await _context.Orders
                .Include(o => o.OrderItems)
                .Where(o => o.BuyerId == userId)
                .OrderByDescending(o => o.CreatedDate)
                .ToListAsync();
        }
        // 🔹 Atomik commit için eklenen metod
        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        public async Task<Domain.Entities.Order> GetByIdAsync(int id)
        {
            return await _context.Orders.FindAsync(id);
        }
    }
}
