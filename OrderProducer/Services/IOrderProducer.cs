using Schemas;

namespace OrderProducer.Services;

public interface IOrderProducer
{
    Task CreateOrder(Order order);
}