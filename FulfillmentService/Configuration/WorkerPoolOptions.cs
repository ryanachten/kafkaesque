namespace FulfillmentService.Configuration;

public sealed class WorkerPoolOptions
{
    public const string SectionName = "WorkerPool";

    public int WorkerCount { get; set; } = 5;
    public int QueueCapacity { get; set; } = 100;
}
