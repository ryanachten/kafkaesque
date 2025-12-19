using Schemas;

namespace OrderService.Services;

public interface IOrderProducer
{
    Task CreateOrder(Order order);
}