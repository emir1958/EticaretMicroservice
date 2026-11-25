namespace EticaretMicroservice.Services.Order.Application.Interfaces
{
    public interface IOrderRepository
    {
        Task<Domain.Entities.Order> AddAsync(Domain.Entities.Order order);
        Task<List<Domain.Entities.Order>> GetOrdersByUserIdAsync(string userId);
        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
        Task<Domain.Entities.Order> GetByIdAsync(int id);
        Task<T> ExecuteInTransactionAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken = default);
    }
}
