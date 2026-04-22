using FulfillmentService.Models;

namespace FulfillmentService.Repositories;

public interface IFulfillmentOrderRepository
{
    Task<bool> UpdateFulfilledStatus(string orderShortCode, CancellationToken cancellationToken = default);
}