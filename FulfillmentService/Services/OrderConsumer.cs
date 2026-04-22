using Common;
using FulfillmentService.Models;
using Schemas;
using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Microsoft.Extensions.Options;
using static Common.Constants;

namespace FulfillmentService.Services;

public sealed class OrderConsumer : BackgroundService
{
    private readonly IConsumer<string, OrderPlaced> _consumer;
    private readonly IOrderWorkerPool _workerPool;
    private readonly ILogger<OrderConsumer> _logger;

    public OrderConsumer(
        IOptions<KafkaConfiguration> kafkaOptions,
        IOrderWorkerPool queueProcessor,
        ILogger<OrderConsumer> logger)
    {
        _workerPool = queueProcessor;
        _logger = logger;

        var kafkaConfig = kafkaOptions.Value;

        var groupId = Environment.GetEnvironmentVariable("GROUP_ID");
        if (groupId == null || groupId == string.Empty)
        {
            throw new ArgumentException("GROUP_ID cannot be empty");
        }

        var schemaRegistryConfig = new SchemaRegistryConfig()
        {
            Url = kafkaConfig.SchemaRegistryUrl
        };

        var consumerConfig = new ConsumerConfig()
        {
            GroupId = groupId,
            BootstrapServers = kafkaConfig.BootstrapServers,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false,
        };

        var schemaRegistryClient = new CachedSchemaRegistryClient(schemaRegistryConfig);

        _consumer = new ConsumerBuilder<string, OrderPlaced>(consumerConfig)
            .SetValueDeserializer(new AvroDeserializer<OrderPlaced>(schemaRegistryClient).AsSyncOverAsync())
            .Build();

        _consumer.Subscribe(Topics.OrderPlaced);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var response = _consumer.Consume(stoppingToken);
                    if (response.Message != null)
                    {
                        var order = new Order(response.Message.Value);
                        await _workerPool.EnqueueOrder(order, stoppingToken);
                        _consumer.Commit(response);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Exception consuming message {Message}", ex.Message);
                    throw;
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Exception enqueueing consumed order message");
                    throw;
                }
            }
        }
        finally
        {
            _consumer.Close();
        }
    }

    public override void Dispose()
    {
        _consumer.Dispose();
        base.Dispose();
    }
}