using FulfillmentService.Models;

namespace FulfillmentService.Services;

public interface IOrderWorkerPool
{
    Task EnqueueOrder(Order order, CancellationToken cancellationToken);
}
