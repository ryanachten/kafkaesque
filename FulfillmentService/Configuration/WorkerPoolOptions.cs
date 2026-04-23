using System.ComponentModel.DataAnnotations;

namespace FulfillmentService.Configuration;

public sealed class WorkerPoolOptions
{
    public const string SectionName = "WorkerPool";

    [Range(1, int.MaxValue)]
    public int WorkerCount { get; set; } = 5;

    [Range(1, int.MaxValue)]
    public int QueueCapacity { get; set; } = 100;
}
