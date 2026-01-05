namespace OrderService.Configuration;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    public int PollingIntervalMilliseconds { get; set; }
    public int ProcessingBatchSize { get; set; }
}
