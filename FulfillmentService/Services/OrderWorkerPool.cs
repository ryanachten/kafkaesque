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

    private async Task ProcessOrders(int workerId, CancellationToken cancellationToken)
    {
        await foreach (var order in _orderQueue.Reader.ReadAllAsync(cancellationToken))
        {
            try
            {
                _logger.LogInformation("Worker {WorkerId} processing order {OrderId}", workerId, order.OrderShortCode);
                await _orderProcessingService.FulfillOrder(order, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Worker {WorkerId} failed to process order {OrderId}", workerId, order.OrderShortCode);
            }
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _orderQueue.Writer.Complete();
        await base.StopAsync(cancellationToken);
    }
}
