using EticaretMicroservice.Services.Order.Application.Interfaces;
using EticaretMicroservice.Services.Order.Domain.Enums;
using EticaretMicroservice.Services.Order.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.OrderItems)
                .Where(o => o.BuyerId == userId)
                .OrderByDescending(o => o.CreatedDate)
                .ToListAsync();
        }

        public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<Domain.Entities.Order?> GetByIdAsync(int id)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                // Saga, geçmişe bakarak kopya kaydı engeller: History her zaman yüklenmeli
                .Include(o => o.History)
                .AsSplitQuery()
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        // Transaction, execution strategy içinde açılır: bağlantı hatasında tüm blok baştan denenebilir
        public async Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    var result = await action();
                    await transaction.CommitAsync(cancellationToken);
                    return result;
                }
                catch
                {
                    await transaction.RollbackAsync(cancellationToken);
                    throw;
                }
            });
        }
    }
}
