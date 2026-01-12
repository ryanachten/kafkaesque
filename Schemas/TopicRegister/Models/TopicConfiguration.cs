namespace TopicRegister.Models;

public class TopicConfigurationFile
{
    public List<TopicDefinition> Topics { get; set; } = [];
}

public class TopicDefinition
{
    /// <summary>
    /// The name of the Kafka topic to create.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The number of partitions for the topic.
    /// More partitions allow for greater parallelism.
    /// </summary>
    public int Partitions { get; set; }

    /// <summary>
    /// The replication factor for the topic.
    /// Determines how many copies of each partition are maintained across brokers for fault tolerance.
    /// </summary>
    public int ReplicationFactor { get; set; } = 1;

    /// <summary>
    /// How long messages are retained in the topic before being deleted, in milliseconds.
    /// Common values: 604800000 (7 days, Kafka default), 86400000 (1 day), -1 (infinite).
    /// </summary>
    public long? RetentionMs { get; set; }

    /// <summary>
    /// The cleanup policy for the topic.
    /// "delete" - Delete old messages based on time/size (Kafka default)
    /// "compact" - Keep only the latest value per key.
    /// </summary>
    public string? CleanupPolicy { get; set; }
}
