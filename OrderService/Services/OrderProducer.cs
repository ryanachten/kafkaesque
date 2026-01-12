using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using static Common.Constants;
using Common;
using Microsoft.Extensions.Options;
using Schemas;
using OrderService.Models;

namespace OrderService.Services;

public sealed class OrderProducer : IOrderProducer, IDisposable
{
    private readonly IProducer<string, OrderPlaced> _producer;
    private readonly ILogger<OrderProducer> _logger;

    public OrderProducer(IOptions<KafkaConfiguration> kafkaOptions, ILogger<OrderProducer> logger)
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

        var schemaRegistryClient = new CachedSchemaRegistryClient(schemaRegistryConfig);

        // NOTE: AutoRegisterSchemas is disabled to avoid auto-generation issues.
        // The schema must be registered in the Schema Registry first.
        var avroSerializerConfig = new AvroSerializerConfig
        {
            AutoRegisterSchemas = false,
            UseLatestVersion = true
        };

        _producer = new ProducerBuilder<string, OrderPlaced>(producerConfig)
            .SetValueSerializer(new AvroSerializer<OrderPlaced>(schemaRegistryClient, avroSerializerConfig))
            .Build();

        _logger = logger;
    }

    public async Task ProduceOrderPlacedEvent(OrderPlaced order, EventMetadata metadata)
    {
        try
        {
            await _producer.ProduceAsync(Topics.OrderPlaced, new Message<string, OrderPlaced>()
            {
                Key = order.OrderShortCode,
                Value = order,
                Headers = metadata.ToKafkaHeaders()
            });
        }
        catch (ProduceException<string, OrderPlaced> ex)
        {
            _logger.LogError("Failed to produce order: {Reason}", ex.Error.Reason);
            throw;
        }
    }

    public void Dispose()
    {
        _producer.Dispose();
    }
}