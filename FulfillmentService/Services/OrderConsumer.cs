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
    private readonly CachedSchemaRegistryClient _schemaRegistryClient;
    private readonly ConsumerRetryConfiguration _retryConfig;
    private readonly KafkaConfiguration _kafkaConfig;
    private readonly ILogger<OrderConsumer> _logger;

    public OrderConsumer(
        IOptions<KafkaConfiguration> kafkaOptions,
        IOptions<ConsumerRetryConfiguration> retryOptions,
        ILogger<OrderConsumer> logger)
    {
        _kafkaConfig = kafkaOptions.Value;
        _retryConfig = retryOptions.Value;
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
            EnableAutoCommit = false,
            MaxPollIntervalMs = _retryConfig.MaxPollIntervalMs
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

        _deadLetterProducer = new ProducerBuilder<string, OrderPlaced>(producerConfig)
            .SetValueSerializer(new AvroSerializer<OrderPlaced>(_schemaRegistryClient))
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

                        var processedSuccessfully = await ProcessOrderWithRetry(orderPlaced, metadata, response);

                        if (processedSuccessfully)
                        {
                            _consumer.Commit(response);
                            _logger.LogInformation(
                                "Processed order {OrderShortCode} and committed offset",
                                orderPlaced.OrderShortCode);
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

    private async Task<bool> ProcessOrderWithRetry(OrderPlaced order, EventMetadata metadata, ConsumeResult<string, OrderPlaced> response)
    {
        var retryCount = 0;
        var delay = _retryConfig.InitialRetryDelayMs;

        while (retryCount < _retryConfig.MaxRetryAttempts)
        {
            try
            {
                _logger.LogInformation("Processing order {OrderShortCode} with {ItemCount} items",
                    order.OrderShortCode, order.Items.Count);
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                retryCount++;
                if (retryCount >= _retryConfig.MaxRetryAttempts)
                {
                    _logger.LogError(
                        "Failed to process order {OrderShortCode} after {Attempts} attempts: {Ex}. Sending to dead-letter queue",
                        order.OrderShortCode, retryCount, ex);
                    await SendToDeadLetterQueue(order, metadata, ex, response);
                    return false;
                }

                _logger.LogWarning(
                    "Failed to process order {OrderShortCode} (attempt {Attempt}/{MaxAttempts}): {Ex}. Retrying in {DelayMs}ms",
                    order.OrderShortCode, retryCount, _retryConfig.MaxRetryAttempts, ex, delay);
                await Task.Delay(delay);
                delay = (int)(delay * _retryConfig.BackoffMultiplier);
                delay = Math.Min(delay, _retryConfig.MaxRetryDelayMs);
            }
        }

        return false;
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