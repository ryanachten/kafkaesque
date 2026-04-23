namespace FulfillmentService.Models;

public enum OrderStatus
{
    Pending = 1,
    Fulfilled = 2,
    Shipped = 3
}

public static class OrderStatusExtensions
{
    public static string ToDbString(this OrderStatus status) => status switch
    {
        OrderStatus.Pending => "PENDING",
        OrderStatus.Fulfilled => "FULFILLED",
        OrderStatus.Shipped => "SHIPPED",
        _ => status.ToString().ToUpperInvariant()
    };

    public static OrderStatus FromDbString(string value) => value?.ToUpperInvariant() switch
    {
        "PENDING" => OrderStatus.Pending,
        "FULFILLED" => OrderStatus.Fulfilled,
        "SHIPPED" => OrderStatus.Shipped,
        _ => throw new ArgumentException($"Unknown status value: {value}", nameof(value))
    };
}