# Data Model: Fulfillment Order Events

## Entities

### Order (modified)

| Field | Type | Description |
|-------|------|------------|
| OrderShortCode | string | Unique order identifier (10-char NanoId) |
| CustomerId | Guid | Customer who placed the order |
| Items | List<OrderItem> | Items in the order |
| Status | OrderStatus | Current order status |
| FulfilledAt | DateTime? | When fulfillment completed (NEW) |

### OrderStatus (enum)

| Value | Description |
|-------|-------------|
| PENDING | Order placed, not yet fulfilled |
| FULFILLED | Order fulfilled |
| SHIPPED | Order shipped |

### OrderItem

| Field | Type | Description |
|-------|------|------------|
| ProductId | Guid | Product identifier |
| Count | int | Quantity ordered |

---

## CDC Event (via Debezium)

Debezium emits events to Kafka with `ExtractNewRecordState` transform (flattened payload).

### CDC Topic

`dbserver.public.orders`

### CDC Payload (Flattened)

| Field | Type | Description |
|-------|------|-------------|
| order_short_code | string | Unique order identifier |
| status | string | PENDING, FULFILLED, or SHIPPED |
| fulfilled_at | timestamp | When fulfillment completed (null for PENDING) |

---

## State Transitions

```text
[PENDING] --fulfillment complete--> [FULFILLED]
[FULFILLED] --------cannot fulfill again--------> (reject duplicate)
```

---

## Validation Rules

1. **FR-001**: FulfilledAt MUST be set when order transitions to FULFILLED
2. **FR-002**: Once FulfilledAt is set, it MUST NOT be overwritten
3. **FR-004**: CDC MUST capture order status changes automatically