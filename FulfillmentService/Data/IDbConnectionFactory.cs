using System.Data;

namespace FulfillmentService.Data;

public interface IDbConnectionFactory
{
    Task<IDbConnection> CreateConnection(CancellationToken cancellationToken = default);
}