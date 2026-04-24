using Schemas;

namespace FulfillmentService.Services;

public interface IOrderFulfilledProducer
{
    Task ProduceOrderFulfilled(OrderFulfilled order, EventMetadata metadata);
}