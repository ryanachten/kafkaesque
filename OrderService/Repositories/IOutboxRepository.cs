using System.Data;
using OrderService.Models;

namespace OrderService.Repositories;

public interface IOutboxRepository
{
    Task CreateOutboxEvent(OutboxEvent outboxEvent, IDbTransaction transaction);
    Task<IEnumerable<OutboxEvent>> GetAndUpdatePendingEvents(EventType eventType);
}