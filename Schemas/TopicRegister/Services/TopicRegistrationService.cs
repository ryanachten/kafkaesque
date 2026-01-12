using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TopicRegister.Models;
using TopicRegister.Options;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace TopicRegister.Services;

public class TopicRegistrationService(
    IConfiguration configuration,
    IOptions<TopicRegistrationOptions> options,
    ILogger<TopicRegistrationService> logger) : ITopicRegistrationService
{
    private const string TopicsConfigPath = "kafka-topics.yml";
    private readonly IConfiguration _configuration = configuration;
    private readonly TopicRegistrationOptions _options = options.Value;
    private readonly ILogger<TopicRegistrationService> _logger = logger;

    public async Task WaitForKafka(CancellationToken cancellationToken = default)
    {
        var bootstrapServers = _configuration["Kafka:BootstrapServers"]
            ?? throw new InvalidOperationException("Kafka BootstrapServers is not configured");

        var maxRetries = _options.MaxRetries;
        var retryDelay = TimeSpan.FromSeconds(_options.RetryDelaySeconds);

        _logger.LogInformation("Waiting for Kafka broker at {Servers} to be ready...", bootstrapServers);

        var config = new AdminClientConfig { BootstrapServers = bootstrapServers };

        for (int i = 1; i <= maxRetries; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using var adminClient = new AdminClientBuilder(config).Build();
                var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(5));

                if (metadata.Brokers.Count > 0)
                {
                    _logger.LogInformation("Kafka broker is ready");
                    return;
                }
            }
            catch (Exception)
            {
            }

            _logger.LogInformation("Kafka not ready yet (attempt {Attempt}/{MaxRetries}), waiting {Delay} seconds...",
                i, maxRetries, retryDelay.TotalSeconds);
            await Task.Delay(retryDelay, cancellationToken);
        }

        throw new InvalidOperationException($"Kafka broker at {bootstrapServers} did not become ready after {maxRetries} attempts");
    }

    public async Task RegisterTopics(CancellationToken cancellationToken = default)
    {
        var configPath = Path.Combine(Directory.GetCurrentDirectory(), TopicsConfigPath);

        if (!File.Exists(configPath))
        {
            _logger.LogWarning("Topic configuration file not found at {Path}. No topics to register.", configPath);
            return;
        }

        var yamlContent = await File.ReadAllTextAsync(configPath, cancellationToken);

        var deserializer = new DeserializerBuilder()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

        var topicConfig = deserializer.Deserialize<TopicConfigurationFile>(yamlContent);

        if (topicConfig?.Topics == null || topicConfig.Topics.Count == 0)
        {
            _logger.LogWarning("No topics defined in configuration file");
            return;
        }

        _logger.LogInformation("Found {Count} topic(s) to register", topicConfig.Topics.Count);

        var bootstrapServers = _configuration["Kafka:BootstrapServers"]
            ?? throw new InvalidOperationException("Kafka BootstrapServers is not configured");

        var adminConfig = new AdminClientConfig { BootstrapServers = bootstrapServers };

        using var adminClient = new AdminClientBuilder(adminConfig).Build();

        var existingTopics = await GetExistingTopics(adminClient, cancellationToken);

        foreach (var topic in topicConfig.Topics)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await RegisterTopicAsync(adminClient, topic, existingTopics, cancellationToken);
        }

        _logger.LogInformation("Topic registration complete");
    }

    private async Task<HashSet<string>> GetExistingTopics(IAdminClient adminClient, CancellationToken cancellationToken)
    {
        try
        {
            var metadata = adminClient.GetMetadata(TimeSpan.FromSeconds(10));
            return metadata.Topics.Select(t => t.Topic).ToHashSet();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to retrieve existing topics, proceeding with registration");
            return [];
        }
    }

    private async Task RegisterTopicAsync(
        IAdminClient adminClient,
        TopicDefinition topic,
        HashSet<string> existingTopics,
        CancellationToken cancellationToken)
    {
        if (existingTopics.Contains(topic.Name))
        {
            _logger.LogInformation("Topic '{TopicName}' already exists, skipping", topic.Name);
            return;
        }

        _logger.LogInformation("Creating topic: {TopicName} (Partitions: {Partitions}, Replication: {Replication})",
            topic.Name, topic.Partitions, topic.ReplicationFactor);

        var configs = new Dictionary<string, string>();

        if (topic.RetentionMs.HasValue)
            configs["retention.ms"] = topic.RetentionMs.Value.ToString();

        if (!string.IsNullOrEmpty(topic.CleanupPolicy))
            configs["cleanup.policy"] = topic.CleanupPolicy;

        var topicSpecification = new TopicSpecification
        {
            Name = topic.Name,
            NumPartitions = topic.Partitions,
            ReplicationFactor = (short)topic.ReplicationFactor,
            Configs = configs.Count > 0 ? configs : null
        };

        try
        {
            await adminClient.CreateTopicsAsync([topicSpecification]);
            _logger.LogInformation("Topic '{TopicName}' created successfully", topic.Name);
        }
        catch (CreateTopicsException ex)
        {
            var result = ex.Results.FirstOrDefault(r => r.Topic == topic.Name);

            if (result?.Error.Code == ErrorCode.TopicAlreadyExists)
            {
                _logger.LogInformation("Topic '{TopicName}' already exists", topic.Name);
            }
            else
            {
                _logger.LogError(ex, "Failed to create topic '{TopicName}': {Error}", topic.Name, result?.Error.Reason);
                throw;
            }
        }
    }
}
