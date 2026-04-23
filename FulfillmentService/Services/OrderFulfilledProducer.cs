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

    public OrderFulfilledProducer(IOptions<KafkaConfiguration> kafkaOptions, ILogger<OrderFulfilledProducer> logger)
    {
        var kafkaConfig = kafkaOptions.Value;
        var producerConfig = new ProducerConfig()
        {
            BootstrapServers = kafkaConfig.BootstrapServers,
        };

        var schemaRegistryConfig = new SchemaRegistryConfig()
        {
            Url = kafkaConfig.SchemaRegistryUrl
        };

        _schemaRegistryClient = new CachedSchemaRegistryClient(schemaRegistryConfig);

        var avroSerializerConfig = new AvroSerializerConfig
        {
            AutoRegisterSchemas = false,
            UseLatestVersion = true
        };

        _producer = new ProducerBuilder<string, OrderFulfilled>(producerConfig)
            .SetValueSerializer(new AvroSerializer<OrderFulfilled>(_schemaRegistryClient, avroSerializerConfig))
            .Build();

        _logger = logger;
    }

    public async Task ProduceOrderFulfilledEvent(OrderFulfilled order, EventMetadata metadata)
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
        }
        catch (ProduceException<string, OrderFulfilled> ex)
        {
            _logger.LogError("Failed to produce OrderFulfilled event: {Reason}", ex.Error.Reason);
            throw;
        }
    }

    public void Dispose()
    {
        _producer.Dispose();
        _schemaRegistryClient.Dispose();
    }
}