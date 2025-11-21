using Common;
using Common.Models;
using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Microsoft.Extensions.Options;
using static Common.Constants;

namespace KafkaConsumer.Services.OrderConsumer;

public sealed class OrderConsumer : BackgroundService
{
    private readonly IConsumer<Null, Order> _consumer;

    public OrderConsumer(IOptions<KafkaConfiguration> kafkaOptions)
    {
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

        _consumer = new ConsumerBuilder<Null, Order>(consumerConfig)
            // TODO: this sync over async approach sucks, also lets look at Avro serialization instead of JSON 
            .SetValueDeserializer(new JsonDeserializer<Order>(schemaRegistryClient).AsSyncOverAsync())
            .Build();

        _consumer.Subscribe(Topics.Orders);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine("Messages will appear below:");

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var response = _consumer.Consume(stoppingToken);
                    if (response.Message != null)
                    {
                        Console.WriteLine($"Received order with {response.Message.Value.Items.Count} items");
                    }
                }
                catch (ConsumeException ex)
                {
                    Console.WriteLine($"Exception consuming message {ex.Message}");
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