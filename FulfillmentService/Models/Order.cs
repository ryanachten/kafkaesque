using Schemas;

namespace FulfillmentService.Models;

public class Order
{
    public string OrderShortCode { get; }
    public Guid CustomerId { get; }
    public IEnumerable<OrderItem> Items { get; }

    public Order(OrderPlaced eventData)
    {
        OrderShortCode = eventData.OrderShortCode;
        CustomerId = Guid.Parse(eventData.CustomerId);

        Items = eventData.Items.Select(item => new OrderItem(
            Guid.Parse(item.ProductId),
            item.Count)
        );
    }
}

public record OrderItem(Guid ProductId, int Count);
