using OrderService.Models;
using Schemas;

namespace OrderService.Services;

public interface IOrderService
{
    Task<Order> CreateOrder(Order order);
}