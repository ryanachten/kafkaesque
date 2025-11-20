using Common.Models;

namespace KafkaProducer.Services.OrderProducer;

public interface IOrderProducer
{
    Task CreateOrder(Order order);
}