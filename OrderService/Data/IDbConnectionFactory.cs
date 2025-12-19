using System.Data;

namespace OrderService.Data;

public interface IDbConnectionFactory
{
    Task<IDbConnection> CreateConnection();
}
