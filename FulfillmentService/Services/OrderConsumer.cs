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
        };

        var schemaRegistryClient = new CachedSchemaRegistryClient(schemaRegistryConfig);

        _consumer = new ConsumerBuilder<string, OrderPlaced>(consumerConfig)
            .SetValueDeserializer(new AvroDeserializer<OrderPlaced>(schemaRegistryClient).AsSyncOverAsync())
            .Build();

        _consumer.Subscribe(Topics.OrderPlaced);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Order consumer started, messages will appear below:");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var response = _consumer.Consume(stoppingToken);
                    if (response.Message != null)
                    {
                        var eventData = response.Message.Value;

                        _logger.LogInformation("Received order {OrderShortCode} with {ItemCount} items",
                            eventData.OrderShortCode,
                            eventData.Items.Count);

                        await _workerPool.EnqueueOrder(new Order(eventData), stoppingToken);
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError(ex, "Exception consuming message {Message}", ex.Message);
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