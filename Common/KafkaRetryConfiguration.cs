namespace Common;

public class KafkaRetryConfiguration
{
    public const string SectionName = "KafkaRetry";

    public int MaxRetryAttempts { get; set; } = 3;
    public int InitialRetryDelayMs { get; set; } = 100;
    public int MaxRetryDelayMs { get; set; } = 10000;
    public double BackoffMultiplier { get; set; } = 2.0;
    public int MessageTimeoutMs { get; set; } = 30000;
    public string Acks { get; set; } = "All";
}