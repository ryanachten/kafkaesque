namespace OrderService.Configuration;

public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    /// <summary>
    /// Frequency for how often the worker will attempt to publish events
    /// </summary>
    public int PollingIntervalMilliseconds { get; set; }

    /// <summary>
    /// Number of events to process and publish in a single worker cycle
    /// </summary>
    public int ProcessingBatchSize { get; set; }

    /// <summary>
    /// How many times an outbox event will attempt to be published before being added to the dead-letter queue 
    /// </summary>
    public int RetryLimit { get; set; }
}
