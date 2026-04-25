using Common;
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
    private readonly IProducer<string, OrderPlaced> _deadLetterProducer;
    private readonly IOrderFulfillmentService _fulfillmentService;
    private readonly CachedSchemaRegistryClient _schemaRegistryClient;
    private readonly KafkaConfiguration _kafkaConfig;
    private readonly ILogger<OrderConsumer> _logger;

    public OrderConsumer(
        IOptions<KafkaConfiguration> kafkaOptions,
        IOrderFulfillmentService fulfillmentService,
        ILogger<OrderConsumer> logger)
    {
        _kafkaConfig = kafkaOptions.Value;
        _fulfillmentService = fulfillmentService;
        _logger = logger;

        var groupId = Environment.GetEnvironmentVariable("GROUP_ID");
        if (groupId == null || groupId == string.Empty)
        {
            throw new ArgumentException("GROUP_ID cannot be empty");
        }

        var schemaRegistryConfig = new SchemaRegistryConfig()
        {
            Url = _kafkaConfig.SchemaRegistryUrl
        };

        _schemaRegistryClient = new CachedSchemaRegistryClient(schemaRegistryConfig);

        var consumerConfig = new ConsumerConfig()
        {
            GroupId = groupId,
            BootstrapServers = _kafkaConfig.BootstrapServers,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        _consumer = new ConsumerBuilder<string, OrderPlaced>(consumerConfig)
            .SetValueDeserializer(new AvroDeserializer<OrderPlaced>(_schemaRegistryClient).AsSyncOverAsync())
            .SetErrorHandler(OnErrorHandler)
            .Build();

        var producerConfig = new ProducerConfig()
        {
            BootstrapServers = _kafkaConfig.BootstrapServers,
            Acks = Acks.All
        };

        var avroSerializerConfig = new AvroSerializerConfig
        {
            AutoRegisterSchemas = false,
            UseLatestVersion = true,
            SubjectNameStrategy = SubjectNameStrategy.Record
        };

        _deadLetterProducer = new ProducerBuilder<string, OrderPlaced>(producerConfig)
            .SetValueSerializer(new AvroSerializer<OrderPlaced>(_schemaRegistryClient, avroSerializerConfig))
            .Build();

        _consumer.Subscribe(Topics.OrderPlaced);
    }

    private void OnErrorHandler(IClient client, Error error)
    {
        if (error.IsFatal)
        {
            _logger.LogCritical("Fatal consumer error: {Reason}", error.Reason);
            return;
        }
        _logger.LogWarning("Consumer non-fatal error: {Reason}", error.Reason);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OrderConsumer started");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var response = _consumer.Consume(stoppingToken);
                    if (response?.Message?.Value is OrderPlaced orderPlaced)
                    {
                        var metadata = EventMetadata.FromKafkaHeaders(response.Message.Headers);
                        _logger.LogInformation(
                            "Received OrderPlaced event for order {OrderShortCode}, EventId: {EventId}",
                            orderPlaced.OrderShortCode,
                            metadata.EventId);

                        var result = await _fulfillmentService.ProcessOrder(orderPlaced, metadata, stoppingToken);

                        if (result.Success)
                        {
                            _consumer.Commit(response);
                            _logger.LogInformation(
                                "Processed order {OrderShortCode} and committed offset",
                                orderPlaced.OrderShortCode);
                        }
                        else if (result.Error != null)
                        {
                            await SendToDeadLetterQueue(orderPlaced, metadata, result.Error, response);
                        }
                    }
                }
                catch (ConsumeException ex)
                {
                    _logger.LogError("Error consuming message: {Message}", ex.Message);
                }
            }
        }
        finally
        {
            _consumer.Close();
        }
    }

    private async Task SendToDeadLetterQueue(OrderPlaced order, EventMetadata metadata, Exception exception, ConsumeResult<string, OrderPlaced> response)
    {
        try
        {
            var dlqMetadata = new DeadLetterMetadata(
                Topics.OrderPlaced,
                response.Partition.Value,
                response.Offset.Value,
                exception);

            var headers = metadata.ToKafkaHeaders();
            headers.AddDeadLetterHeaders(dlqMetadata);

            await _deadLetterProducer.ProduceAsync(Constants.DeadLetterTopics.FulfillmentService, new Message<string, OrderPlaced>
            {
                Key = order.OrderShortCode,
                Value = order,
                Headers = headers
            });

            _logger.LogWarning(
                "Sent order {OrderShortCode} to dead-letter queue at {DeadLetterTopic}",
                order.OrderShortCode,
                Constants.DeadLetterTopics.FulfillmentService);
        }
        catch (Exception dlqEx)
        {
            _logger.LogCritical(
                "Failed to send order {OrderShortCode} to dead-letter queue: {Ex}",
                order.OrderShortCode,
                dlqEx);
        }
    }

    public override void Dispose()
    {
        _consumer.Dispose();
        _deadLetterProducer.Dispose();
        _schemaRegistryClient.Dispose();
        base.Dispose();
    }
}