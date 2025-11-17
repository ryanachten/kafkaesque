using Confluent.Kafka;
using Common;
using static Common.Constants;

var builder = WebApplication.CreateBuilder(args);

var kafkaConfig = builder.Configuration.GetRequiredSection(KafkaConfiguration.SectionName).Get<KafkaConfiguration>()
    ?? throw new InvalidOperationException("Kafka configuration is missing");

/// Allows for demonstration of how consumers are treated differently
/// when they are part of the same group, versus when they have different group IDs
/// - If consumers have the same group ID, messages should be allocated between them
/// - If consumers have different group IDs, they should receive all messages in a broadcast-fashion
var groupId = Environment.GetEnvironmentVariable("GROUP_ID");
if (groupId == null || groupId == string.Empty)
{
    throw new ArgumentException("GROUP_ID cannot be empty");
}

var config = new ConsumerConfig()
{
    GroupId = groupId,
    BootstrapServers = kafkaConfig.BootstrapServers,
    AutoOffsetReset = AutoOffsetReset.Earliest,
};

using var consumer = new ConsumerBuilder<Null, string>(config).Build();
using var tokenSource = new CancellationTokenSource();

consumer.Subscribe(Topics.Orders);

Console.WriteLine("Messages will appear below:");
try
{
    while (true)
    {
        var response = consumer.Consume(tokenSource.Token);
        if(response.Message != null)
        {
            Console.WriteLine(response.Message.Value);
        }
    }
}
catch (ConsumeException ex)
{
    Console.WriteLine($"Exception consuming message {ex.Message}");
    throw;
}