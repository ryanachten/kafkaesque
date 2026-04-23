namespace Common;

public class ConsumerRetryConfiguration
{
    public const string SectionName = "ConsumerRetry";

    public int MaxRetryAttempts { get; set; } = 3;
    public int InitialRetryDelayMs { get; set; } = 100;
    public int MaxRetryDelayMs { get; set; } = 10000;
    public double BackoffMultiplier { get; set; } = 2.0;
    public int MaxPollIntervalMs { get; set; } = 300000;
}