using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using static Common.Constants;
using Common;
using Microsoft.Extensions.Options;
using Schemas;

namespace OrderService.Services;

public sealed class OrderProducer : IOrderProducer, IDisposable
{
    private readonly IProducer<Null, OrderPlaced> _producer;

    public OrderProducer(IOptions<KafkaConfiguration> kafkaOptions)
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

        // Note: AutoRegisterSchemas is disabled to avoid auto-generation issues.
        // The schema must be registered in the Schema Registry first.
        var avroSerializerConfig = new AvroSerializerConfig
        {
            AutoRegisterSchemas = false,
            UseLatestVersion = true
        };

        _producer = new ProducerBuilder<Null, OrderPlaced>(producerConfig)
            .SetValueSerializer(new AvroSerializer<OrderPlaced>(schemaRegistryClient, avroSerializerConfig))
            .Build();
    }

    public async Task ProduceOrderPlacedEvent(OrderPlaced order)
    {
        try
        {
            await _producer.ProduceAsync(Topics.Orders, new Message<Null, OrderPlaced>()
            {
                Value = order
            });
        }
        catch (ProduceException<Null, OrderPlaced> ex)
        {
            Console.WriteLine($"Failed to produce order: {ex.Error.Reason}");
            throw;
        }
    }

    public void Dispose()
    {
        _producer.Dispose();
    }
}