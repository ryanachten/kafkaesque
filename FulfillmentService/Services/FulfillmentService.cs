using FulfillmentService.Models;

namespace FulfillmentService.Services;

public sealed class FulfillmentService(ILogger<FulfillmentService> logger) : IFulfillmentService
{
    private readonly Random _random = new();
    private readonly ILogger<FulfillmentService> _logger = logger;
    private const int MaxFulfillmentTime = 10;

    public async Task FulfillOrder(Order order, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing order {OrderShortCode} with {ItemCount} items", order.OrderShortCode, order.Items.Count());

        var fulfillmentTime = TimeSpan.FromSeconds(_random.Next(MaxFulfillmentTime));
        await Task.Delay(fulfillmentTime, cancellationToken);

        // TODO: update order status

        _logger.LogInformation("Completed processing order {OrderShortCode} in {FulfillmentTime} seconds", order.OrderShortCode, fulfillmentTime);
    }
}
