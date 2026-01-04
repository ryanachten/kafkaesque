using Dapper;
using OrderService.Data;
using OrderService.Models;

namespace OrderService.Repositories;

public class OrderRepository(IDbConnectionFactory connectionFactory, ILogger<OrderRepository> logger) : IOrderRepository
{
    public async Task<Order> Create(Order order)
    {
        const string insertOrderSql = @"
            INSERT INTO orders (order_short_code, customer_id, status, created_at, updated_at)
            VALUES (@OrderShortCode, @CustomerId, @Status, NOW(), NOW())
            RETURNING order_id";

        const string insertOrderItemSql = @"
            INSERT INTO order_items (order_id, product_id, count)
            VALUES (@OrderId, @ProductId, @Count)";

        using var connection = await connectionFactory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            var orderId = await connection.QuerySingleAsync<Guid>(
                insertOrderSql,
                new { order.OrderShortCode, order.CustomerId, Status = order.Status.ToString() },
                transaction: transaction);

            foreach (var item in order.Items)
            {
                await connection.ExecuteAsync(
                    insertOrderItemSql,
                    new { OrderId = orderId, item.ProductId, item.Count },
                    transaction: transaction);
            }

            transaction.Commit();

            return order;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<Order?> GetById(string orderId)
    {
        const string sql = @"
            SELECT o.order_short_code AS OrderShortCode, 
                   o.customer_id AS CustomerId, 
                   o.status AS Status,
                   oi.product_id AS ProductId,
                   oi.count AS Count
            FROM orders o
            LEFT JOIN order_items oi ON o.order_id = oi.order_id
            WHERE o.order_short_code = @OrderId";

        logger.LogInformation("Getting order with ID: {OrderId}", orderId);

        using var connection = await connectionFactory.CreateConnection();
        var orderDict = new Dictionary<string, Order>();

        await connection.QueryAsync<Order, OrderItem, Order>(
            sql,
            (order, item) =>
            {
                if (!orderDict.TryGetValue(order.OrderShortCode, out var existingOrder))
                {
                    existingOrder = order;
                    existingOrder.Items = new List<OrderItem>();
                    orderDict.Add(order.OrderShortCode, existingOrder);
                }

                if (item != null)
                {
                    ((List<OrderItem>)existingOrder.Items).Add(item);
                }

                return existingOrder;
            },
            new { OrderId = orderId },
            splitOn: "ProductId");

        return orderDict.Values.FirstOrDefault();
    }

    public async Task<IEnumerable<Order>> GetAll()
    {
        const string sql = @"
            SELECT o.order_short_code AS OrderShortCode, 
                   o.customer_id AS CustomerId, 
                   o.status AS Status,
                   oi.product_id AS ProductId,
                   oi.count AS Count
            FROM orders o
            LEFT JOIN order_items oi ON o.order_id = oi.order_id
            ORDER BY o.created_at DESC";

        logger.LogInformation("Getting all orders");

        using var connection = await connectionFactory.CreateConnection();
        var orderDict = new Dictionary<string, Order>();

        await connection.QueryAsync<Order, OrderItem, Order>(
            sql,
            (order, item) =>
            {
                if (!orderDict.TryGetValue(order.OrderShortCode, out var existingOrder))
                {
                    existingOrder = order;
                    existingOrder.Items = new List<OrderItem>();
                    orderDict.Add(order.OrderShortCode, existingOrder);
                }

                if (item != null)
                {
                    ((List<OrderItem>)existingOrder.Items).Add(item);
                }

                return existingOrder;
            },
            splitOn: "ProductId");

        return orderDict.Values;
    }

    public async Task<bool> Update(Order order)
    {
        const string updateOrderSql = @"
            UPDATE orders 
            SET customer_id = @CustomerId, 
                status = @Status,
                updated_at = NOW()
            WHERE order_short_code = @OrderShortCode";

        const string deleteOrderItemsSql = @"
            DELETE FROM order_items 
            WHERE order_id = (SELECT order_id FROM orders WHERE order_short_code = @OrderShortCode)";

        const string insertOrderItemSql = @"
            INSERT INTO order_items (order_id, product_id, count)
            SELECT order_id, @ProductId, @Count
            FROM orders 
            WHERE order_short_code = @OrderShortCode";

        logger.LogInformation("Updating order in database");

        using var connection = await connectionFactory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            var rowsAffected = await connection.ExecuteAsync(
                updateOrderSql,
                new { order.OrderShortCode, order.CustomerId, Status = order.Status.ToString() },
                transaction: transaction);

            if (rowsAffected == 0)
            {
                transaction.Rollback();
                return false;
            }

            await connection.ExecuteAsync(
                deleteOrderItemsSql,
                new { order.OrderShortCode },
                transaction: transaction);

            foreach (var item in order.Items)
            {
                await connection.ExecuteAsync(
                    insertOrderItemSql,
                    new { order.OrderShortCode, item.ProductId, item.Count },
                    transaction: transaction);
            }

            transaction.Commit();
            return true;
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public async Task<bool> Delete(string orderId)
    {
        const string sql = "DELETE FROM orders WHERE order_short_code = @OrderId";

        logger.LogInformation("Deleting order with ID: {OrderId}", orderId);

        using var connection = await connectionFactory.CreateConnection();
        var rowsAffected = await connection.ExecuteAsync(sql, new { OrderId = orderId });
        return rowsAffected > 0;
    }
}
