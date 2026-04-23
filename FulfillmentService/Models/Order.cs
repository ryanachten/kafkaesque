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
        if (!Guid.TryParse(eventData.CustomerId, out var customerId))
        {
            throw new ArgumentException("Invalid customer id.", nameof(eventData));
        }
        CustomerId = customerId;
        Status = OrderStatus.Pending;
        FulfilledAt = null;

        Items = eventData.Items.Select(item =>
        {
            if (!Guid.TryParse(item.ProductId, out var productId))
            {
                throw new ArgumentException("Invalid product id.", nameof(eventData));
            }
            return new OrderItem(productId, item.Count);
        }).ToList();
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
