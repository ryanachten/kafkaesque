using FulfillmentService.Models;

namespace FulfillmentService.Services;

public interface IFulfillmentService
{
    Task FulfillOrder(Order order, CancellationToken cancellationToken = default);
}
