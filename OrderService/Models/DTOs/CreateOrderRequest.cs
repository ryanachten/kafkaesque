namespace OrderService.Models.DTOs;

public record CreateOrderRequest(Guid CustomerId, IEnumerable<CreateOrderRequestItem> Items);

public record CreateOrderRequestItem(Guid ProductId, int Count);