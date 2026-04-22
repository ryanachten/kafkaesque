using Dapper;
using FulfillmentService.Data;
using FulfillmentService.Models;

namespace FulfillmentService.Repositories;

public class FulfillmentOrderRepository(IDbConnectionFactory connectionFactory) : IFulfillmentOrderRepository
{
    private const string UpdateFulfilledStatusSql = @"
        UPDATE orders
        SET status = @NewStatus,
            fulfilled_at = COALESCE(fulfilled_at, NOW()),
            updated_at = NOW()
        WHERE order_short_code = @OrderShortCode
          AND status != @NewStatus
        RETURNING order_id";

    private const string CheckStatusSql = @"
        SELECT status, fulfilled_at
        FROM orders
        WHERE order_short_code = @OrderShortCode";

    public async Task<bool> UpdateFulfilledStatus(string orderShortCode, CancellationToken cancellationToken = default)
    {
        using var connection = await connectionFactory.CreateConnection();

        var existingOrder = await connection.QuerySingleOrDefaultAsync<dynamic>(
            CheckStatusSql,
            new { OrderShortCode = orderShortCode });

        if (existingOrder == null)
        {
            return false;
        }

        var currentStatus = existingOrder.status?.ToString();
        if (currentStatus == OrderStatus.Fulfilled.ToString() || existingOrder.fulfilled_at != null)
        {
            return false;
        }

        var result = await connection.ExecuteAsync(
            UpdateFulfilledStatusSql,
            new { OrderShortCode = orderShortCode, NewStatus = OrderStatus.Fulfilled.ToString() });

        return result > 0;
    }
}