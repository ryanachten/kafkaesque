using Common;
using Schemas;
using Confluent.Kafka;
using Confluent.Kafka.SyncOverAsync;
using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Microsoft.Extensions.Options;
using static Common.Constants;

namespace OrderConsumer.Services;

public sealed class OrderConsumer : BackgroundService
{
    private readonly IConsumer<Null, OrderPlaced> _consumer;

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

        _consumer = new ConsumerBuilder<Null, OrderPlaced>(consumerConfig)
            .SetValueDeserializer(new AvroDeserializer<OrderPlaced>(schemaRegistryClient).AsSyncOverAsync())
            .Build();

        _consumer.Subscribe(Topics.Orders);
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
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

        return Task.CompletedTask;
    }

    public override void Dispose()
    {
        _consumer.Dispose();
        base.Dispose();
    }
}