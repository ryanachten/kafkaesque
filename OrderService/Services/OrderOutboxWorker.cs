
using Microsoft.Extensions.Options;
using OrderService.Configuration;
using OrderService.Helpers;
using OrderService.Models;
using OrderService.Repositories;

namespace OrderService.Services;

public sealed class OrderOutboxWorker(IServiceProvider serviceProvider, IOptions<OutboxOptions> outboxOptions) : IHostedService, IDisposable
{
    private Timer? _timer;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // TODO: decide if periodic timer or timer is more appropriate here
        _timer = new Timer(async _ =>
        {
            await PublishEvents();
        }, null, TimeSpan.Zero, TimeSpan.FromMilliseconds(outboxOptions.Value.PollingIntervalMilliseconds));

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _timer?.Change(Timeout.Infinite, 0);

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
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
    }
}