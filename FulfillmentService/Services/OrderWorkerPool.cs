using FulfillmentService.Configuration;
using FulfillmentService.Models;
using Microsoft.Extensions.Options;
using System.Threading.Channels;

namespace FulfillmentService.Services;

public sealed class OrderWorkerPool : BackgroundService, IOrderWorkerPool
{
    private readonly IFulfillmentService _orderProcessingService;
    private readonly ILogger<OrderWorkerPool> _logger;
    private readonly Channel<Order> _orderQueue;
    private readonly int _workerCount;
    private readonly List<Task> _workerTasks = [];

    public OrderWorkerPool(
        IOptions<WorkerPoolOptions> workerPoolOptions,
        IFulfillmentService orderProcessingService,
        ILogger<OrderWorkerPool> logger)
    {
        _orderProcessingService = orderProcessingService;
        _logger = logger;

        var workerConfig = workerPoolOptions.Value;
        if (workerConfig.WorkerCount <= 0)
        {
            throw new InvalidOperationException("WorkerPool:WorkerCount must be greater than 0.");
        }

        if (workerConfig.QueueCapacity <= 0)
        {
            throw new InvalidOperationException("WorkerPool:QueueCapacity must be greater than 0.");
        }

        _workerCount = workerConfig.WorkerCount;

        _orderQueue = Channel.CreateBounded<Order>(new BoundedChannelOptions(workerConfig.QueueCapacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        });
    }

    public async Task EnqueueOrder(Order order, CancellationToken cancellationToken = default)
    {
        await _orderQueue.Writer.WriteAsync(order, cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        for (int i = 0; i < _workerCount; i++)
        {
            int workerId = i + 1;
            _workerTasks.Add(Task.Run(() => ProcessOrders(workerId, stoppingToken), stoppingToken));
        }

        await Task.WhenAll(_workerTasks);
    }

    private const int MaxRetries = 3;

    private async Task ProcessOrders(int workerId, CancellationToken cancellationToken)
    {
        await foreach (var order in _orderQueue.Reader.ReadAllAsync(cancellationToken))
        {
            var attempts = 0;
            while (attempts < MaxRetries)
            {
                try
                {
                    _logger.LogInformation("Worker {WorkerId} processing order {OrderId}, attempt {Attempt}",
                        workerId, order.OrderShortCode, attempts + 1);
                    await _orderProcessingService.FulfillOrder(order, cancellationToken);
                    break;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    attempts++;
                    _logger.LogWarning(ex,
                        "Worker {WorkerId} failed to process order {OrderId}, attempt {Attempt}/{MaxRetries}",
                        workerId, order.OrderShortCode, attempts, MaxRetries);

                    if (attempts >= MaxRetries)
                    {
                        _logger.LogError(ex,
                            "Worker {WorkerId} exhausted retries for order {OrderId}, message may be lost",
                            workerId, order.OrderShortCode);
                        throw;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempts)), cancellationToken);
                }
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _orderQueue.Writer.Complete();
        await _orderQueue.Reader.Completion;
        await Task.WhenAll(_workerTasks);
        await base.StopAsync(cancellationToken);
    }
}
