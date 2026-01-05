using System.Text.Json;
using OrderService.Models;
using Schemas;

namespace OrderService.Helpers;

public static class OutboxEventSerializer
{
    public static OutboxEvent FromOrder(Order order)
    {
        return new OutboxEvent
        {
            Id = Guid.NewGuid(),
            EntityName = EntityName.ORDER,
            EntityId = order.OrderShortCode,
            EventType = EventType.ORDER_PLACED,
            EventVersion = 0, // TODO: what is the best way to determine this?
            OccurredAt = DateTime.UtcNow,
            Payload = JsonSerializer.Serialize(order)
        };
    }

    public static OrderPlaced? ToOrderPlaced(OutboxEvent outboxEvent)
    {
        // TODO: the metadata should really be encoded in the Kafka event somehow
        var order = JsonSerializer.Deserialize<Order>(outboxEvent.Payload);

        if (order is null) return null;

        return new OrderPlaced()
        {
            // CustomerId = order.CustomerId, // TODO: need to update the event schema to support this stuff
            Items = [.. order.Items.Select(i => new OrderPlacedItem()
            {
                // ProductId = i.ProductId,  // TODO: need to update the event schema to support this stuff
                Name = "REMOVE ME",
                Count = i.Count,
            })]
        };
    }
}
