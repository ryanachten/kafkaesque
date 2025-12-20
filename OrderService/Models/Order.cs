namespace OrderService.Models;

public record Order(IEnumerable<OrderItem> Items);

public record OrderItem(string Name, int Count);