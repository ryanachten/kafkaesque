# Contract: OrderFulfilled Event

## Overview

| Aspect | Detail |
|--------|--------|
| Event Name | OrderFulfilled |
| Topic | order.fulfilled |
| Schema Registry | Schemas.OrderFulfilled |
| Publisher | FulfillmentService |
| Subscriber | OrderService |
| Delivery Semantics | At-least-once |

## Event Schema

```avro
{
  "type": "record",
  "name": "OrderFulfilled",
  "namespace": "Schemas",
  "fields": [
    {
      "name": "orderShortCode",
      "type": "string",
      "doc": "Unique order identifier matching OrderPlaced event"
    },
    {
      "name": "customerId",
      "type": "string",
      "doc": "Customer identifier matching OrderPlaced event"
    },
    {
      "name": "fulfillmentTimestamp",
      "type": "long",
      "doc": "Unix timestamp in milliseconds when fulfillment completed"
    }
  ]
}
```

**Note**: Event metadata (correlationId, timestamp, sourceService) is passed via Kafka headers, consistent with OrderPlaced event pattern.

## Publisher Contract (FulfillmentService)

### Responsibilities

1. **Schema Registration**: Register OrderFulfilled schema in Schema Registry before publishing
2. **Event Publishing**: Publish to `order.fulfilled` topic with key = orderShortCode
3. **Headers**: Include event metadata in Kafka headers for tracing
4. **Error Handling**: Retry on transient failures, dead-letter on persistent failures

### Expected Behavior

| Scenario | Behavior |
|----------|----------|
| Successful fulfillment | Publish OrderFulfilled event with headers |
| Broker unavailable | Retry with exponential backoff |
| Schema not registered | Fail fast with clear error message |

## Subscriber Contract (OrderService)

### Responsibilities

1. **Topic Subscription**: Subscribe to `order.fulfilled` topic via consumer group
2. **Headers Processing**: Read metadata from Kafka headers
3. **Event Processing**: Update order status to FULFILLED in database
4. **Idempotency**: Handle duplicate events gracefully
5. **Error Handling**: Log warnings for unknown orders, retry on transient DB errors

### Expected Behavior

| Scenario | Behavior |
|----------|----------|
| Valid event, order exists | Update status to FULFILLED |
| Valid event, order not found | Log warning, acknowledge (don't rethrow) |
| Duplicate event | Idempotent - no error, status already FULFILLED |
| Schema mismatch | Log error, send to dead-letter |

## Kafka Headers

| Header Key | Type | Description |
|-----------|------|-------------|
| event-id | string | Unique event identifier for tracing |
| event-version | string | Event schema version |
| occurred-at | string | ISO8601 timestamp when event occurred |
| entity-type | string | Type of entity (Order) |
| entity-id | string | Entity identifier (orderShortCode) |

## Consumer Group Configuration

| Setting | Value | Rationale |
|---------|-------|-----------|
| Group ID | order-service-fulfilled-consumer | Shared group for scaling |
| Auto Offset Reset | Earliest | Don't miss events on restart |
| Enable Auto Commit | False | Manual commit after processing |

## Event Flow

```
┌─────────────────────┐     order.fulfilled      ┌─────────────────────┐
│  FulfillmentService │ ────────────────────────► │   OrderService     │
│                     │        + Headers        │                     │
│ 1. Receive         │                          │ 1. Subscribe       │
│    OrderPlaced     │                          │ 2. Deserialize     │
│ 2. Process order  │                          │ 3. Read headers    │
│ 3. Mark complete  │                          │ 4. Validate        │
│ 4. Publish event  │                          │ 5. Update DB       │
│    with headers   │                          │ 6. Commit offset  │
└─────────────────────┘                          └─────────────────────┘
```

## Version History

| Version | Changes |
|---------|---------|
| 1.0.0 | Initial version (simplified - metadata via headers) |