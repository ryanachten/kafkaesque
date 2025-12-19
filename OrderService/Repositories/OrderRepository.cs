using Dapper;
using OrderService.Data;
using Schemas;

namespace OrderService.Repositories;

public class OrderRepository(IDbConnectionFactory connectionFactory, ILogger<OrderRepository> logger) : IOrderRepository
{
    public async Task<Order> Create(Order order)
    {
        const string insertOrderSql = @"
            INSERT INTO orders (order_id, created_at, updated_at)
            VALUES (gen_random_uuid(), NOW(), NOW())
            RETURNING order_id";

        const string insertOrderItemSql = @"
            INSERT INTO order_items (order_id, name, count)
            VALUES (@OrderId, @Name, @Count)";

        using var connection = await connectionFactory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        try
        {
            var orderId = await connection.QuerySingleAsync<Guid>(insertOrderSql, transaction: transaction);

            foreach (var item in order.Items)
            {
                await connection.ExecuteAsync(
                    insertOrderItemSql,
                    new { OrderId = orderId, item.Name, item.Count },
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
        // TODO: Implement SQL query to get order by ID
        // Example:
        // const string sql = "SELECT * FROM orders WHERE order_id = @OrderId";

        logger.LogInformation("Getting order with ID: {OrderId}", orderId);

        // using var connection = await _connectionFactory.CreateConnectionAsync();
        // var result = await connection.QuerySingleOrDefaultAsync<Order>(sql, new { OrderId = orderId });
        // return result;

        await Task.CompletedTask; // Remove warning
        throw new NotImplementedException("GetByIdAsync not yet implemented - awaiting schema definition");
    }

    public async Task<IEnumerable<Order>> GetAll()
    {
        // TODO: Implement SQL query to get all orders
        // Example:
        // const string sql = "SELECT * FROM orders ORDER BY timestamp DESC";

        logger.LogInformation("Getting all orders");

        // using var connection = await _connectionFactory.CreateConnectionAsync();
        // var results = await connection.QueryAsync<Order>(sql);
        // return results;

        await Task.CompletedTask; // Remove warning
        throw new NotImplementedException("GetAllAsync not yet implemented - awaiting schema definition");
    }

    public async Task<bool> Update(Order order)
    {
        // TODO: Implement SQL query to update order
        // Example:
        // const string sql = @"
        //     UPDATE orders 
        //     SET customer_name = @CustomerName, 
        //         product = @Product, 
        //         quantity = @Quantity, 
        //         price = @Price
        //     WHERE order_id = @OrderId";

        logger.LogInformation("Updating order in database");

        // using var connection = await _connectionFactory.CreateConnectionAsync();
        // var rowsAffected = await connection.ExecuteAsync(sql, order);
        // return rowsAffected > 0;

        await Task.CompletedTask; // Remove warning
        throw new NotImplementedException("UpdateAsync not yet implemented - awaiting schema definition");
    }

    public async Task<bool> Delete(string orderId)
    {
        // TODO: Implement SQL query to delete order
        // Example:
        // const string sql = "DELETE FROM orders WHERE order_id = @OrderId";

        logger.LogInformation("Deleting order with ID: {OrderId}", orderId);

        // using var connection = await _connectionFactory.CreateConnectionAsync();
        // var rowsAffected = await connection.ExecuteAsync(sql, new { OrderId = orderId });
        // return rowsAffected > 0;

        await Task.CompletedTask; // Remove warning
        throw new NotImplementedException("DeleteAsync not yet implemented - awaiting schema definition");
    }
}
