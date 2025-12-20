using OrderService.Models;
using OrderService.Repositories;

namespace OrderService.Services;

public class OrderService(IOrderRepository orderRepository) : IOrderService
{
    public async Task<Order> CreateOrder(Order order)
    {
        return await orderRepository.Create(order);
    }
}