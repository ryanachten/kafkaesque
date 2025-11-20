namespace Common.Models;

public class Order
{
    public required List<OrderItem> Items { get; set; }
}
