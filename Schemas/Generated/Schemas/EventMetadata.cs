using Confluent.Kafka;

namespace Schemas;

public class EventMetadata
{
    public required Guid EventId { get; init; }
    public required int EventVersion { get; init; }
    public required DateTime OccurredAt { get; init; }
    public required string EntityType { get; init; }
    public required string EntityId { get; init; }

    public Headers ToKafkaHeaders()
    {
        return new Headers
        {
            { "event-id", System.Text.Encoding.UTF8.GetBytes(EventId.ToString()) },
            { "event-version", System.Text.Encoding.UTF8.GetBytes(EventVersion.ToString()) },
            { "occurred-at", System.Text.Encoding.UTF8.GetBytes(OccurredAt.ToString("O")) },
            { "entity-type", System.Text.Encoding.UTF8.GetBytes(EntityType) },
            { "entity-id", System.Text.Encoding.UTF8.GetBytes(EntityId) }
        };
    }

    public static EventMetadata FromKafkaHeaders(Headers? headers)
    {
        if (headers is null) return CreateDefault();

        var hasEventId = TryGetHeaderValue(headers, "event-id", out var eventId);
        var hasEventVersion = TryGetHeaderValue(headers, "event-version", out var eventVersion);
        var hasOccurredAt = TryGetHeaderValue(headers, "occurred-at", out var occurredAt);
        var hasEntityType = TryGetHeaderValue(headers, "entity-type", out var entityType);
        var hasEntityId = TryGetHeaderValue(headers, "entity-id", out var entityId);

        if (!hasEventId || !hasEventVersion || !hasOccurredAt ||
            !hasEntityType || !hasEntityId)
            return CreateDefault();

        return new EventMetadata
        {
            EventId = Guid.Parse(eventId),
            EventVersion = int.Parse(eventVersion),
            OccurredAt = DateTime.Parse(occurredAt),
            EntityType = entityType,
            EntityId = entityId
        };
    }

    private static EventMetadata CreateDefault()
    {
        return new EventMetadata
        {
            EventId = Guid.NewGuid(),
            EventVersion = 1,
            OccurredAt = DateTime.UtcNow,
            EntityType = "Order",
            EntityId = string.Empty
        };
    }

    public static EventMetadata FromValues(Guid eventId, int eventVersion, DateTime occurredAt, string entityType, string entityId)
    {
        return new EventMetadata
        {
            EventId = eventId,
            EventVersion = eventVersion,
            OccurredAt = occurredAt,
            EntityType = entityType,
            EntityId = entityId
        };
    }

    private static bool TryGetHeaderValue(Headers headers, string key, out string value)
    {
        var header = headers.FirstOrDefault(h => h.Key == key);
        if (header?.GetValueBytes() is byte[] bytes)
        {
            value = System.Text.Encoding.UTF8.GetString(bytes);
            return true;
        }

        value = string.Empty;
        return false;
    }
}