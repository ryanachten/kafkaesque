using Newtonsoft.Json;

namespace KafkaProducer.Models;

public class Order
{
    [JsonRequired]
    [JsonProperty("items")]
    public required List<OrderItem> Items { get; set; }
}
