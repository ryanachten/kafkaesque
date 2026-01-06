
using Microsoft.Extensions.Options;
using OrderService.Configuration;
using OrderService.Helpers;
using OrderService.Models;
using OrderService.Repositories;

namespace OrderService.Services;

public sealed class OrderOutboxWorker(
    IServiceProvider serviceProvider,
    IOptions<OutboxOptions> outboxOptions,
    ILogger<OrderOutboxWorker> logger) : IHostedService, IDisposable
{
    private PeriodicTimer? _timer;
    private Task? _executingTask;
    private CancellationTokenSource? _tokenSource;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _timer = new PeriodicTimer(TimeSpan.FromMilliseconds(outboxOptions.Value.PollingIntervalMilliseconds));
        _tokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _executingTask = ExecuteAsync(_tokenSource.Token);

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_tokenSource is not null) await _tokenSource.CancelAsync();

        if (_executingTask is not null) await _executingTask;
    }

    public void Dispose()
    {
        _tokenSource?.Dispose();
        _timer?.Dispose();
    }

    private async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (await _timer!.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await PublishEvents();
                }
                catch (Exception ex)
                {
                    logger.LogError("Error occurred while processing outbox {Ex}", ex);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task PublishEvents()
    {
        using var scope = serviceProvider.CreateScope();

        var orderProducer = scope.ServiceProvider.GetRequiredService<IOrderProducer>();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();

        var pendingOutboxEvents = await outboxRepository.GetAndUpdatePendingEvents(EventType.ORDER_PLACED);

        if (!pendingOutboxEvents.Any()) return;

        logger.LogInformation("Received {Count} pending outbox events for publishing", pendingOutboxEvents.Count());

        List<Guid> successfulEventIds = [];
        Dictionary<Guid, string> failedEventIdsAndErrors = [];

        // TODO: should this be done in parallel or could that result in out of order processing?
        foreach (var outboxEvent in pendingOutboxEvents)
        {
            var producerEvent = OutboxEventSerializer.ToOrderPlaced(outboxEvent);
            if (producerEvent is null) continue;

            try
            {
                await orderProducer.ProduceOrderPlacedEvent(producerEvent);
                successfulEventIds.Add(outboxEvent.Id);
            }
            catch (Exception ex)
            {
                failedEventIdsAndErrors.Add(outboxEvent.Id, ex.Message);
            }
        }

        await Task.WhenAll(
            outboxRepository.UpdateEventsAsPublished(successfulEventIds),
            outboxRepository.UpdateEventsAsFailed(failedEventIdsAndErrors));

        if (successfulEventIds.Count > 0) logger.LogInformation("Marked {PublishedCount} events published", successfulEventIds.Count);
        if (failedEventIdsAndErrors.Count > 0) logger.LogWarning("Marked {FailedCount} events as failed", failedEventIdsAndErrors.Count);

        // TODO: we need to reprocess failed events within a certain retry threshold on a separate schedule and increment the retry count
    }
}