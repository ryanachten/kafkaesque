using Schemas;

namespace FulfillmentService.Services;

public interface IOrderFulfilledProducer
{
    Task ProduceOrderFulfilledEventAsync(OrderFulfilled order, EventMetadata metadata);
}