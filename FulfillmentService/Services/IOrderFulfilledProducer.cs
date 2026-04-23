using Schemas;

namespace FulfillmentService.Services;

public interface IOrderFulfilledProducer
{
    Task ProduceOrderFulfilledEvent(OrderFulfilled order, EventMetadata metadata);
}