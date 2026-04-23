namespace Common;

public static class Constants
{
    /// <summary>
    /// Kafka topics to publish and subscribe to
    /// </summary>
    public static class Topics
    {
        public static readonly string OrderPlaced = "order.placed";
        public static readonly string OrderFulfilled = "order.fulfilled";
    }

    /// <summary>
    /// Dead-letter queue topics for consumer error handling
    /// </summary>
    public static class DeadLetterTopics
    {
        public static readonly string FulfillmentService = "fulfillment-service.dlq";
        public static readonly string OrderService = "order-service.dlq";
    }
}
