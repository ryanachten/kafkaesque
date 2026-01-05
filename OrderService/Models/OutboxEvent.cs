namespace OrderService.Models;

public class OutboxEvent
{
    public required Guid Id { get; set; }

    public required EntityName EntityName { get; set; }
    public required string EntityId { get; set; }

    public required EventType EventType { get; set; }
    public required int EventVersion { get; set; }

    public required string Payload { get; set; }

    public required DateTime OccurredAt { get; set; }
    public OutboxEventStatus Status { get; set; } = OutboxEventStatus.PENDING;
    public DateTime? PublishedAt { get; set; }

    public int RetryCount { get; set; } = 0;
    public string? LastError { get; set; }
}

public enum EntityName
{
    ORDER
}

public enum EventType
{
    ORDER_PLACED
}

public enum OutboxEventStatus
{
    PENDING,
    PROCESSING,
    PUBLISHED,
    FAILED
}