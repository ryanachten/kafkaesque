using Schemas;

namespace OrderService.Repositories;

public interface IOrderRepository
{
    Task<Order> CreateAsync(Order order);

    Task<Order?> GetByIdAsync(string orderId);

    Task<IEnumerable<Order>> GetAllAsync();

    Task<bool> UpdateAsync(Order order);

    Task<bool> DeleteAsync(string orderId);
}
