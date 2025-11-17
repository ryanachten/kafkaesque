using Confluent.Kafka;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using KafkaProducer.Models;
using static Common.Constants;
using Common;
using Microsoft.Extensions.Options;

namespace KafkaProducer.Services.OrderProducer;

public class OrderProducer : IOrderProducer
{
    private readonly KafkaConfiguration _kafkaConfig;

    public OrderProducer(IOptions<KafkaConfiguration> kafkaConfig)
    {
        _kafkaConfig = kafkaConfig.Value;
    }

    public async Task CreateOrder(Order order)
    {
        var producerConfig = new ProducerConfig()
        {
            BootstrapServers = _kafkaConfig.BootstrapServers,
        };

        var schemaRegistryConfig = new SchemaRegistryConfig()
        {
            Url = _kafkaConfig.SchemaRegistryUrl
        };

        using var schemaRegistryClient = new CachedSchemaRegistryClient(schemaRegistryConfig);
        
        // Note: AutoRegisterSchemas is disabled to avoid auto-generation issues.
        // The schema must be manually registered in the Schema Registry first.
        // See: Scripts/register-schemas.ps1 for schema registration
        var jsonSerializerConfig = new JsonSerializerConfig
        {
            AutoRegisterSchemas = false,
            UseLatestVersion = true
        };
        
        using var producer = new ProducerBuilder<Null, Order>(producerConfig)
            .SetValueSerializer(new JsonSerializer<Order>(schemaRegistryClient, jsonSerializerConfig))
            .Build();

        try
        {
            await producer.ProduceAsync(Topics.Orders, new Message<Null, Order>()
            {
                Value = order
            });
        }
        catch (ProduceException<Null, Order> ex)
        {
            Console.WriteLine($"Failed to produce order: {ex.Error.Reason}");
            throw;
        }
    }
}