using System.Data;
using OrderService.Models;

namespace OrderService.Repositories;

public interface IOutboxRepository
{
    Task CreateOutboxEvent(OutboxEvent outboxEvent, IDbTransaction transaction);
    Task<IEnumerable<OutboxEvent>> GetAndUpdatePendingEvents(EventType eventType);
    Task UpdateEventsAsPublished(IEnumerable<Guid> eventIds);
    Task UpdateEventsAsFailed(Dictionary<Guid, string> eventIdsAndErrorMessages);
}