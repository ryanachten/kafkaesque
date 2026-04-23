using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Microsoft.Extensions.Options;
using OrderService.Models;
using OrderService.Repositories;
using static Common.Constants;
using Common;
using Schemas;

namespace OrderService.Services;

public sealed class OrderFulfilledConsumer : IHostedService, IDisposable
{
    private readonly IConsumer<string, OrderFulfilled> _consumer;
    private readonly IOrderRepository _orderRepository;
    private readonly ILogger<OrderFulfilledConsumer> _logger;
    private readonly CancellationTokenSource _tokenSource;
    private Task? _executingTask;

    public OrderFulfilledConsumer(
        IOptions<KafkaConfiguration> kafkaOptions,
        IOrderRepository orderRepository,
        ILogger<OrderFulfilledConsumer> logger)
    {
        var kafkaConfig = kafkaOptions.Value;
        var groupId = "order-service-fulfilled-consumer";

        var schemaRegistryConfig = new SchemaRegistryConfig()
        {
            Url = kafkaConfig.SchemaRegistryUrl
        };

        var consumerConfig = new ConsumerConfig()
        {
            GroupId = groupId,
            BootstrapServers = kafkaConfig.BootstrapServers,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        var schemaRegistryClient = new CachedSchemaRegistryClient(schemaRegistryConfig);

        _consumer = new ConsumerBuilder<string, OrderFulfilled>(consumerConfig)
            .SetValueDeserializer(new AvroDeserializer<OrderFulfilled>(schemaRegistryClient).AsSyncOverAsync())
            .Build();

        _consumer.Subscribe(Topics.OrderFulfilled);

        _orderRepository = orderRepository;
        _logger = logger;
        _tokenSource = new CancellationTokenSource();
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _executingTask = ExecuteAsync(_tokenSource.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_tokenSource is not null)
        {
            await _tokenSource.CancelAsync();
        }

        if (_executingTask is not null)
        {
            await _executingTask;
        }
    }

    public void Dispose()
    {
        _consumer.Dispose();
        _tokenSource.Dispose();
    }

    private async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var response = _consumer.Consume(stoppingToken);
                    if (response.Message?.Value is OrderFulfilled orderFulfilled)
                    {
                        var metadata = EventMetadata.FromKafkaHeaders(response.Message.Headers);
                        _logger.LogInformation(
                            "Received OrderFulfilled event for order {OrderShortCode}, EventId: {EventId}",
                            orderFulfilled.OrderShortCode,
                            metadata?.EventId ?? Guid.Empty);

                        try
                        {
                            await _orderRepository.UpdateStatus(orderFulfilled.OrderShortCode, OrderStatus.FULFILLED);
                            _consumer.Commit(response);
                            _logger.LogInformation(
                                "Updated order {OrderShortCode} to FULFILLED and committed offset",
                                orderFulfilled.OrderShortCode);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(
                                "Failed to update order {OrderShortCode}: {Ex}",
                                orderFulfilled.OrderShortCode,
                                ex);
                        }
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError("Error consuming message: {Message}", ex.Message);
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            _consumer.Close();
        }
    }
}