using Schemas;

namespace FulfillmentService.Models;

public class Order
{
    public string OrderShortCode { get; }
    public Guid CustomerId { get; }
    public IEnumerable<OrderItem> Items { get; }
    public OrderStatus Status { get; set; }
    public DateTime? FulfilledAt { get; set; }

    public Order(OrderPlaced eventData)
    {
        OrderShortCode = eventData.OrderShortCode;
        CustomerId = Guid.Parse(eventData.CustomerId);
        Status = OrderStatus.Pending;
        FulfilledAt = null;

        Items = eventData.Items.Select(item => new OrderItem(
            Guid.Parse(item.ProductId),
            item.Count)
        );
    }

    public Order(string orderShortCode, Guid customerId, OrderStatus status, DateTime? fulfilledAt, IEnumerable<OrderItem> items)
    {
        OrderShortCode = orderShortCode;
        CustomerId = customerId;
        Status = status;
        FulfilledAt = fulfilledAt;
        Items = items;
    }
}

public record OrderItem(Guid ProductId, int Count);
