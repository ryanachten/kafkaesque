# Data Model: Order Completion Event

## Entities

### OrderFulfilled Event

| Field | Type | Required | Description |
|-------|------|----------|-------------|
| orderShortCode | string | Yes | Unique order identifier (matches OrderPlaced) |
| customerId | string | Yes | Customer identifier (matches OrderPlaced) |
| fulfillmentTimestamp | long | Yes | Unix timestamp when fulfillment completed |

**Note**: Event metadata (correlationId, timestamp, sourceService) is passed via Kafka headers, consistent with OrderPlaced event pattern.

## Relationships

```
OrderPlaced (existing)
  │
  ▼
OrderFulfilled (new) ──Kafka Headers──► Tracing/Correlation
  │
  ▼
Order Status Update
  │
  ▼
Order (existing) ──► Status = FULFILLED
```

## State Transitions

### Order Status

```
PENDING ──────► FULFILLED ──────► SHIPPED
```

- Orders start in PENDING status
- Upon receiving OrderFulfilled event, status transitions to FULFILLED
- FULFILLED is an intermediate state before SHIPPED

## Validation Rules

1. **orderShortCode**: Must match an existing order in OrderService database
2. **customerId**: Must correspond to the order's customer
3. **duplicate events**: Must be idempotent - processing the same event twice should not cause issues
4. **unknown order**: If orderShortCode doesn't exist, log warning and acknowledge event (don't rethrow)

## Avro Schema

```avro
{
  "type": "record",
  "name": "OrderFulfilled",
  "namespace": "Schemas",
  "fields": [
    {"name": "orderShortCode", "type": "string"},
    {"name": "customerId", "type": "string"},
    {"name": "fulfillmentTimestamp", "type": "long"}
  ]
}
```

## Kafka Headers

Metadata is passed via Kafka headers (consistent with OrderPlaced):

| Header Key | Type | Description |
|-----------|------|-------------|
| event-id | string | Unique event identifier for tracing |
| event-version | string | Event schema version |
| occurred-at | string | ISO8601 timestamp when event occurred |
| entity-type | string | Type of entity (Order) |
| entity-id | string | Entity identifier (orderShortCode) |