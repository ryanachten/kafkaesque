using OrderService.Repositories;
using Schemas;

namespace OrderService.Services;

public class OrderService(IOrderRepository orderRepository) : IOrderService
{
    // TODO: we probably need to decouple the event schema from the domains models used internally to this service
    public async Task<Order> CreateOrder(Order order)
    {
        return await orderRepository.Create(order);
    }
}