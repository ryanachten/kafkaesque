using OrderService.Models;
using Schemas;

namespace OrderService.Repositories;

public interface IOrderRepository
{
    Task<Order> Create(Order order);

    Task<Order?> GetById(string orderId);

    Task<IEnumerable<Order>> GetAll();

    Task<bool> Update(Order order);

    Task<bool> Delete(string orderId);
}
