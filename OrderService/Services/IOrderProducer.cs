using OrderService.Models;
using Schemas;

namespace OrderService.Services;

public interface IOrderProducer
{
    Task ProduceOrderPlacedEvent(OrderPlaced order, EventMetadata metadata);
}