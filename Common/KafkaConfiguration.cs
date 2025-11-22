namespace Common;

public class KafkaConfiguration
{
    public const string SectionName = "Kafka";

    public required string BootstrapServers { get; set; }
    public required string SchemaRegistryUrl { get; set; }
}
