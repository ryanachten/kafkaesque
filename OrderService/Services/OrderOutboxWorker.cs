
using Microsoft.Extensions.Options;
using OrderService.Configuration;
using OrderService.Helpers;
using OrderService.Models;
using OrderService.Repositories;

namespace OrderService.Services;

public sealed class OrderOutboxWorker(IServiceProvider serviceProvider, IOptions<OutboxOptions> outboxOptions) : IHostedService, IDisposable
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
            await PublishEvents();

            while (await _timer!.WaitForNextTickAsync(stoppingToken))
            {
                await PublishEvents();
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

        var producerEvents = pendingOutboxEvents
            .OrderBy(e => e.OccurredAt)
            .Select(OutboxEventSerializer.ToOrderPlaced);

        // TODO: should this be done in parallel or could that result in out of order processing?
        foreach (var producerEvent in producerEvents)
        {
            if (producerEvent is null) continue;

            await orderProducer.ProduceOrderPlacedEvent(producerEvent);
        }

        // TODO: handle publishing failure by marking event as failed in DB
        // TODO: handle publishing success by marking event as published in DB
        // TODO: consider clean up worker for removing published events
    }
}