using NanoidDotNet;
using OrderService.Models.DTOs;

namespace OrderService.Models;

public class Order
{
    public Order()
    {
        OrderShortCode = Nanoid.Generate(size: 10);
    }

    public Order(CreateOrderRequest dto)
    {
        OrderShortCode = Nanoid.Generate(size: 10);
        CustomerId = dto.CustomerId;
        Items = dto.Items.Select(item => new OrderItem(item.ProductId, item.Count));
    }

    public string OrderShortCode { get; private set; }
    public Guid CustomerId { get; set; }
    public IEnumerable<OrderItem> Items { get; set; } = [];
    public OrderStatus Status { get; set; } = OrderStatus.PENDING;
}

public enum OrderStatus
{
    PENDING,
    FULFILLED,
    SHIPPED
}

public record OrderItem(Guid ProductId, int Count);