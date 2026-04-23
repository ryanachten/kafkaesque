# Quickstart: Order Completion Event Dispatch

## Overview

This feature adds event-driven notification from FulfillmentService to OrderService when order fulfillment completes.

## Architecture

```
┌─────────────────────┐     order.fulfilled      ┌─────────────────────┐
│  FulfillmentService │ ────────────────────────► │   OrderService     │
│                     │                          │                     │
│ • OrderConsumer    │                          │ • OrderOutboxWorker │
│ • Process order    │                          │ • OrderFulfilled   │
│ • OrderFulfilled   │                          │   Consumer         │
│   Producer          │                          │ • Update status    │
└─────────────────────┘                          └─────────────────────┘
```

## Prerequisites

1. Kafka broker running on configured bootstrap servers
2. Schema Registry available
3. PostgreSQL database accessible
4. .NET 9.0 SDK

## Implementation Steps

### Step 1: Add Topic

Create `order.fulfilled` topic in Kafka (if not exists):

```bash
# Using kafka-topics (adjust based on your setup)
kafka-topics.sh --create \
  --topic order.fulfilled \
  --partitions 3 \
  --replication-factor 1 \
  --bootstrap-servers localhost:9092
```

### Step 2: Generate Schema

Generate Avro schema for OrderFulfilled event:

```bash
cd Schemas/SchemaGenerator
dotnet run -- --schema-name OrderFulfilled --namespace Schemas
```

Or manually create schema file in `Schemas/Generated/Schemas/OrderFulfilled.cs` following the OrderPlaced pattern.

### Step 3: Register Schema

Register the schema in Schema Registry:

```bash
cd Schemas/SchemaRegister
dotnet run -- --schema-path ../schemas/order-fulfilled.avro
```

### Step 4: Implement Producer (FulfillmentService)

Create `OrderFulfilledProducer.cs` in FulfillmentService/Services/:

```csharp
// Key implementation points:
// - Implement IOrderFulfilledProducer interface
// - Use Confluent.Kafka ProducerBuilder
// - Serialize using AvroSerializer<OrderFulfilled>
// - Publish to "order.fulfilled" topic
// - Include correlationId in headers for tracing
```

### Step 5: Implement Consumer (OrderService)

Create `OrderFulfilledConsumer.cs` in OrderService/Services/:

```csharp
// Key implementation points:
// - Implement IBackgroundService
// - Subscribe to "order.fulfilled" topic
// - Use consumer group for scaling
// - Update order status via OutboxRepository
// - Handle idempotency (check if already FULFILLED)
// - Commit offset after successful processing
```

### Step 6: Register Components

Update `Program.cs` files:

**FulfillmentService/Program.cs:**
```csharp
builder.Services.AddSingleton<IOrderFulfilledProducer, OrderFulfilledProducer>();
```

**OrderService/Program.cs:**
```csharp
builder.Services.AddHostedService<OrderFulfilledConsumer>();
```

### Step 7: Update Constants

Add new topic constant in `Common/Constants.cs`:

```csharp
public static class Topics
{
    public static readonly string OrderPlaced = "order.placed";
    public static readonly string OrderFulfilled = "order.fulfilled";  // NEW
}
```

## Testing

### Unit Tests

```bash
dotnet test --filter "OrderFulfilled"
```

### Integration Tests

1. Start Kafka and PostgreSQL
2. Run FulfillmentService
3. Run OrderService
4. Place an order via OrderService API
5. Verify OrderFulfilled event is published
6. Verify order status updates to FULFILLED

## Monitoring

Key metrics to monitor:

| Metric | Target |
|--------|--------|
| Event publishing latency | < 100ms |
| End-to-end status update | < 5 seconds |
| Delivery reliability | 99.9% |
| Duplicate rate | < 0.1% |

## Troubleshooting

| Issue | Solution |
|-------|----------|
| Events not reaching OrderService | Check consumer group subscription and offset |
| Schema validation errors | Verify schema is registered in Schema Registry |
| Duplicate status updates | Check idempotency logic in consumer |
| High latency | Check Kafka broker load and consumer throughput |
