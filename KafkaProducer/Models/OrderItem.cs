using Newtonsoft.Json;

namespace KafkaProducer.Models;

public class OrderItem
{
    [JsonRequired]
    [JsonProperty("name")]
    public required string Name { get; set; }
    [JsonRequired]
    [JsonProperty("count")]
    public required int Count { get; set; }
}