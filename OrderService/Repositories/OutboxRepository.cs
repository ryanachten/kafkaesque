using System.Data;
using Dapper;
using Microsoft.Extensions.Options;
using OrderService.Configuration;
using OrderService.Data;
using OrderService.Models;

namespace OrderService.Repositories;

public class OutboxRepository(IDbConnectionFactory connectionFactory, IOptions<OutboxOptions> outboxOptions) : IOutboxRepository
{
    private const string InsertEventSql = @"
        INSERT INTO outbox_events (id, entity_type, entity_id, event_type, event_version, payload, occurred_at)
        VALUES (@Id, @EntityName, @EntityId, @EventType, @EventVersion, @Payload::jsonb, @OccurredAt)";

    private readonly string _getPendingEventsSql = $@"
        SELECT * FROM outbox_events
        WHERE status = 'PENDING'
        AND event_type = @EventType
        ORDER BY occurred_at
        FOR UPDATE SKIP LOCKED
        LIMIT {outboxOptions.Value.ProcessingBatchSize};
    ";

    private static string GetUpdateEventStatusSql(OutboxEventStatus newStatus) => $@"
        UPDATE outbox_events
        SET status = '{newStatus}'
        WHERE id = ANY(@Ids);
    ";

    private const string MarkEventAsFailedSql = @"
        UPDATE outbox_events
        SET status = 'FAILED', last_error = @ErrorMessage
        WHERE id = @Id;
    ";

    /// <summary>
    /// Inserts an outbox event entry as part of an existing transaction
    /// </summary>
    public async Task CreateOutboxEvent(OutboxEvent outboxEvent, IDbTransaction transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction.Connection);

        await transaction.Connection.ExecuteAsync(
            InsertEventSql,
            new { outboxEvent.Id, outboxEvent.EntityName, outboxEvent.EntityId, EventType = outboxEvent.EventType.ToString(), outboxEvent.EventVersion, outboxEvent.Payload, outboxEvent.OccurredAt },
            transaction: transaction);
    }

    /// <summary>
    /// Gets pending events and marks them as in progress in the same transaction 
    /// </summary>
    /// <param name="eventType">Event type to select for</param>
    /// <returns>Pending events</returns>
    public async Task<IEnumerable<OutboxEvent>> GetAndUpdatePendingEvents(EventType eventType)
    {
        using var connection = await connectionFactory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        var pendingEvents = await connection.QueryAsync<OutboxEvent>(
            _getPendingEventsSql,
            new { EventType = eventType.ToString() },
            transaction: transaction);

        if (pendingEvents.Any())
        {
            var eventIds = pendingEvents.Select(e => e.Id).ToArray();
            await connection.ExecuteAsync(
                GetUpdateEventStatusSql(OutboxEventStatus.PROCESSING),
                new { Ids = eventIds },
                transaction: transaction);
        }

        transaction.Commit();

        return pendingEvents;
    }

    public async Task UpdateEventsAsPublished(IEnumerable<Guid> eventIds)
    {
        using var connection = await connectionFactory.CreateConnection();

        await connection.ExecuteAsync(
            GetUpdateEventStatusSql(OutboxEventStatus.PUBLISHED),
            new { Ids = eventIds });

    }

    public async Task UpdateEventsAsFailed(Dictionary<Guid, string> eventIdsAndErrorMessages)
    {
        using var connection = await connectionFactory.CreateConnection();
        using var transaction = connection.BeginTransaction();

        foreach (var (eventId, errorMessage) in eventIdsAndErrorMessages)
        {
            await connection.ExecuteAsync(
                MarkEventAsFailedSql,
                new { Id = eventId, ErrorMessage = errorMessage },
                transaction: transaction);
        }

        transaction.Commit();
    }
}

