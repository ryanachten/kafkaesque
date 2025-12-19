using System.Data;
using Npgsql;

namespace OrderService.Data;

public class DbConnectionFactory(IConfiguration configuration) : IDbConnectionFactory
{
    private readonly string _connectionString = configuration.GetConnectionString("OrdersDatabase")
            ?? throw new InvalidOperationException("OrdersDatabase connection string is not configured");

    public async Task<IDbConnection> CreateConnection()
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }
}
