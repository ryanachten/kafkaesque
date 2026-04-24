using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using static Common.Constants;
using Common;
using Microsoft.Extensions.Options;
using Schemas;

namespace FulfillmentService.Services;

public sealed class OrderFulfilledProducer : IOrderFulfilledProducer, IDisposable
{
    private readonly IProducer<string, OrderFulfilled> _producer;
    private readonly CachedSchemaRegistryClient _schemaRegistryClient;
    private readonly ILogger<OrderFulfilledProducer> _logger;
    private readonly KafkaRetryConfiguration _retryConfig;
    private readonly KafkaConfiguration _kafkaConfig;

    public OrderFulfilledProducer(
        IOptions<KafkaConfiguration> kafkaOptions,
        IOptions<KafkaRetryConfiguration> retryOptions,
        ILogger<OrderFulfilledProducer> logger)
    {
        _kafkaConfig = kafkaOptions.Value;
        _retryConfig = retryOptions.Value;
        _logger = logger;

        var producerConfig = new ProducerConfig()
        {
            BootstrapServers = _kafkaConfig.BootstrapServers,
            MessageTimeoutMs = _retryConfig.MessageTimeoutMs,
            Acks = Enum.Parse<Acks>(_retryConfig.Acks, ignoreCase: true)
        };

        var schemaRegistryConfig = new SchemaRegistryConfig()
        {
            Url = _kafkaConfig.SchemaRegistryUrl
        };

        _schemaRegistryClient = new CachedSchemaRegistryClient(schemaRegistryConfig);

        var avroSerializerConfig = new AvroSerializerConfig
        {
            AutoRegisterSchemas = false,
            UseLatestVersion = true
        };

        _producer = new ProducerBuilder<string, OrderFulfilled>(producerConfig)
            .SetValueSerializer(new AvroSerializer<OrderFulfilled>(_schemaRegistryClient, avroSerializerConfig))
            .SetErrorHandler(OnErrorHandler)
            .Build();
    }

    private void OnErrorHandler(IClient client, Error error)
    {
        if (error.IsFatal)
        {
            _logger.LogCritical("Fatal producer error: {Reason}", error.Reason);
            return;
        }
        _logger.LogWarning("Producer non-fatal error: {Reason}", error.Reason);
    }

    public async Task ProduceOrderFulfilled(OrderFulfilled order, EventMetadata metadata)
    {
        var retryCount = 0;
        var delay = _retryConfig.InitialRetryDelayMs;

        while (true)
        {
            try
            {
                await _producer.ProduceAsync(Topics.OrderFulfilled, new Message<string, OrderFulfilled>()
                {
                    Key = order.OrderShortCode,
                    Value = order,
                    Headers = metadata.ToKafkaHeaders()
                });

                _logger.LogInformation("Published OrderFulfilled event for order {OrderShortCode}", order.OrderShortCode);
                return;
            }
            catch (ProduceException<string, OrderFulfilled> ex) when (IsTransientError(ex) && retryCount < _retryConfig.MaxRetryAttempts)
            {
                retryCount++;
                _logger.LogWarning(
                    "Transient error publishing OrderFulfilled event (attempt {Attempt}/{MaxAttempts}): {Reason}. Retrying in {DelayMs}ms",
                    retryCount, _retryConfig.MaxRetryAttempts, ex.Error.Reason, delay);

                await Task.Delay(delay);
                delay = (int)(delay * _retryConfig.BackoffMultiplier);
                delay = Math.Min(delay, _retryConfig.MaxRetryDelayMs);
            }
            catch (ProduceException<string, OrderFulfilled> ex)
            {
                _logger.LogError("Failed to produce OrderFulfilled event after {Attempts} attempts: {Reason}",
                    retryCount, ex.Error.Reason);
                throw;
            }
        }
    }

    private static bool IsTransientError(ProduceException<string, OrderFulfilled> ex)
    {
        return ex.Error.IsFatal == false &&
               (ex.Error.Code == ErrorCode.Local_Transport ||
                ex.Error.Code == ErrorCode.Local_AllBrokersDown ||
                ex.Error.Code == ErrorCode.UnknownTopicOrPart);
    }

    public void Dispose()
    {
        _producer.Dispose();
        _schemaRegistryClient.Dispose();
    }
}