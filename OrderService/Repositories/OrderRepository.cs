using Dapper;
using OrderService.Data;
using OrderService.Helpers;
using OrderService.Models;

namespace OrderService.Repositories;

public class OrderRepository(IDbConnectionFactory connectionFactory, IOutboxRepository outboxRepository) : IOrderRepository
{
    private const string InsertOrderSql = @"
        INSERT INTO orders (order_short_code, customer_id, status, created_at, updated_at)
        VALUES (@OrderShortCode, @CustomerId, @Status, NOW(), NOW())
        RETURNING order_id";

    private const string InsertOrderItemSql = @"
        INSERT INTO order_items (order_id, product_id, count)
        VALUES (@OrderId, @ProductId, @Count)";

    private const string UpdateStatusSql = @"
        UPDATE orders
        SET status = @Status, updated_at = NOW()
        WHERE order_short_code = @OrderShortCode";

    public async Task<Order> Create(Order order)
    {
        using var connection = await connectionFactory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            var orderId = await connection.QuerySingleAsync<Guid>(
                InsertOrderSql,
                new { order.OrderShortCode, order.CustomerId, Status = order.Status.ToString() },
                transaction: transaction);

            foreach (var item in order.Items)
            {
                await connection.ExecuteAsync(
                    InsertOrderItemSql,
                    new { OrderId = orderId, item.ProductId, item.Count },
                    transaction: transaction);
            }

            var outboxEvent = OutboxEventSerializer.FromOrder(order);
            await outboxRepository.CreateOutboxEvent(outboxEvent, transaction);

            transaction.Commit();

            return order;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task UpdateStatus(string orderShortCode, OrderStatus status)
    {
        using var connection = await connectionFactory.CreateConnection();
        await connection.ExecuteAsync(UpdateStatusSql, new { OrderShortCode = orderShortCode, Status = status.ToString() });
    }
}
