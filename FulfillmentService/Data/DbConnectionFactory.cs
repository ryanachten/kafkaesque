using System.Data;
using Npgsql;

namespace FulfillmentService.Data;

public class DbConnectionFactory(IConfiguration configuration) : IDbConnectionFactory
{
    private readonly string _connectionString = configuration.GetConnectionString("FulfillmentDatabase")
            ?? throw new InvalidOperationException("FulfillmentDatabase connection string is not configured");

    public async Task<IDbConnection> CreateConnection(CancellationToken cancellationToken = default)
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}