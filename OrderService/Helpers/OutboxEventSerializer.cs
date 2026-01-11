using System.Text.Json;
using OrderService.Models;
using Schemas;

namespace OrderService.Helpers;

public static class OutboxEventSerializer
{
    private static class EventVersions
    {
        public const int OrderPlaced = 1;
    }

    public static OutboxEvent FromOrder(Order order)
    {
        return new OutboxEvent
        {
            Id = Guid.NewGuid(),
            EntityName = EntityName.ORDER,
            EntityId = order.OrderShortCode,
            EventType = EventType.ORDER_PLACED,
            EventVersion = EventVersions.OrderPlaced,
            OccurredAt = DateTime.UtcNow,
            Payload = JsonSerializer.Serialize(order)
        };
    }

    public static OrderPlaced? ToOrderPlaced(OutboxEvent outboxEvent)
    {
        var order = JsonSerializer.Deserialize<Order>(outboxEvent.Payload);

        if (order is null) return null;

        return new OrderPlaced()
        {
            OrderShortCode = order.OrderShortCode,
            CustomerId = order.CustomerId.ToString(),
            Items = [.. order.Items.Select(i => new OrderPlacedItem()
            {
                ProductId = i.ProductId.ToString(),
                Count = i.Count,
            })]
        };
    }
}
