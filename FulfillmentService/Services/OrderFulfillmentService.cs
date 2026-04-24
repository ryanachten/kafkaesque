using Common;
using Microsoft.Extensions.Options;
using Schemas;
using static Common.Constants;

namespace FulfillmentService.Services;

public sealed class OrderFulfillmentService(
    IOrderFulfilledProducer fulfilledProducer,
    IOptions<ConsumerRetryConfiguration> retryOptions,
    ILogger<OrderFulfillmentService> logger) : IOrderFulfillmentService
{
    private readonly IOrderFulfilledProducer _fulfilledProducer = fulfilledProducer;
    private readonly ConsumerRetryConfiguration _retryConfig = retryOptions.Value;
    private readonly ILogger<OrderFulfillmentService> _logger = logger;

    public async Task<OrderFulfillmentResult> ProcessOrder(
        OrderPlaced order,
        EventMetadata metadata,
        CancellationToken cancellationToken)
    {
        var result = await ExecuteWithRetry(order, cancellationToken);

        if (result.Success)
        {
            var fulfilledOrder = new OrderFulfilled
            {
                OrderShortCode = order.OrderShortCode,
                CustomerId = order.CustomerId,
                FulfillmentTimestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            await PublishFulfilledEvent(fulfilledOrder, metadata);
            return new OrderFulfillmentResult(true, fulfilledOrder);
        }

        return result;
    }

    private async Task<OrderFulfillmentResult> ExecuteWithRetry(
        OrderPlaced order,
        CancellationToken cancellationToken)
    {
        var retryCount = 0;
        var delay = _retryConfig.InitialRetryDelayMs;

        while (retryCount < _retryConfig.MaxRetryAttempts)
        {
            try
            {
                await ProcessOrderInternal(order, cancellationToken);
                return new OrderFulfillmentResult(true);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                retryCount++;
                if (retryCount >= _retryConfig.MaxRetryAttempts)
                {
                    _logger.LogError(
                        "Failed to process order {OrderShortCode} after {Attempts} attempts: {Ex}",
                        order.OrderShortCode, retryCount, ex);
                    return new OrderFulfillmentResult(false, Error: ex);
                }

                _logger.LogWarning(
                    "Failed to process order {OrderShortCode} (attempt {Attempt}/{MaxAttempts}): {Ex}. Retrying in {DelayMs}ms",
                    order.OrderShortCode, retryCount, _retryConfig.MaxRetryAttempts, ex, delay);

                await Task.Delay(delay, cancellationToken);
                delay = (int)(delay * _retryConfig.BackoffMultiplier);
                delay = Math.Min(delay, _retryConfig.MaxRetryDelayMs);
            }
        }

        return new OrderFulfillmentResult(false);
    }

    private Task ProcessOrderInternal(OrderPlaced order, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Processing order {OrderShortCode} with {ItemCount} items",
            order.OrderShortCode, order.Items.Count);
        return Task.CompletedTask;
    }

    private async Task PublishFulfilledEvent(OrderFulfilled order, EventMetadata originalMetadata)
    {
        var metadata = EventMetadata.FromValues(
            Guid.NewGuid(),
            1,
            DateTime.UtcNow,
            "Order",
            order.OrderShortCode);

        try
        {
            await _fulfilledProducer.ProduceOrderFulfilled(order, metadata);
            _logger.LogInformation("Published OrderFulfilled event for order {OrderShortCode}", order.OrderShortCode);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to publish OrderFulfilled event for order {OrderShortCode}: {Ex}",
                order.OrderShortCode, ex);
            throw;
        }
    }
}