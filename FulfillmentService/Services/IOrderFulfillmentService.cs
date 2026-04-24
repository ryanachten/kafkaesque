using Schemas;

namespace FulfillmentService.Services;

public interface IOrderFulfillmentService
{
    Task<OrderFulfillmentResult> ProcessOrder(OrderPlaced order, EventMetadata metadata, CancellationToken cancellationToken);
}

public record OrderFulfillmentResult(bool Success, OrderFulfilled? FulfilledOrder = null, Exception? Error = null);