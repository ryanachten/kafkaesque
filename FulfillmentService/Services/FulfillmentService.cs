using FulfillmentService.Models;
using FulfillmentService.Repositories;

namespace FulfillmentService.Services;

public sealed class FulfillmentService(
    ILogger<FulfillmentService> logger,
    IFulfillmentOrderRepository orderRepository) : IFulfillmentService
{
    private readonly Random _random = new();
    private readonly ILogger<FulfillmentService> _logger = logger;
    private readonly IFulfillmentOrderRepository _orderRepository = orderRepository;
    private const int MaxFulfillmentTime = 10;

    public async Task FulfillOrder(Order order, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing order {OrderShortCode} with {ItemCount} items", order.OrderShortCode, order.Items.Count());

        var fulfillmentTime = TimeSpan.FromSeconds(_random.Next(MaxFulfillmentTime));
        await Task.Delay(fulfillmentTime, cancellationToken);

        var updated = await _orderRepository.UpdateFulfilledStatus(order.OrderShortCode, cancellationToken);
        if (!updated)
        {
            _logger.LogWarning("Order {OrderShortCode} already fulfilled or not found", order.OrderShortCode);
            return;
        }

        order.Status = OrderStatus.Fulfilled;
        order.FulfilledAt = DateTime.UtcNow;

        _logger.LogInformation("Completed processing order {OrderShortCode} in {FulfillmentTime} seconds", order.OrderShortCode, fulfillmentTime);
    }
}
