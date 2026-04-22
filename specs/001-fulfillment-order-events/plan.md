# Implementation Plan: Fulfillment Order Events

**Branch**: `[001-fulfillment-order-events]` | **Date**: April 22, 2026 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from fulfillment order events spec

## Summary

This feature extends the FulfillmentService to:
1. **Persist fulfilled timestamp** - Update order records with fulfillment completion time
2. **Notify downstream via CDC** - Use Change Data Capture (Debezium) to emit order state change events

The implementation uses CDC instead of the outbox pattern: database changes are captured by Debezium and streamed to Kafka topics for downstream consumption.

---

## Technical Context

**Language/Version**: C# (.NET 8)
**Primary Dependencies**: Dapper, Npgsql, Serilog
**Storage**: PostgreSQL with Debezium CDC
**Testing**: xUnit (existing test pattern)
**Target Platform**: Linux container with Debezium connector
**Project Type**: Background worker service
**Performance Goals**: Process 100+ orders/second
**Constraints**: CDC latency typically <100ms
**Scale/Scope**: Single service (FulfillmentService) + Debezium connector

---

## Constitution Check

| Gate | Status | Notes |
|------|--------|-------|
| Event-driven pattern | ✅ PASS | Uses CDC (Debezium) |
| Avro schemas for events | ✅ PASS | Debezium can output Avro |
| Incrementally buildable | ✅ PASS | User stories independently testable |
| Learn new patterns | ✅ PASS | CDC is new learning opportunity |

---

## Phase 0: Research & Clarifications

### Decisions Made

| Decision | Rationale | Alternatives Considered |
|----------|-----------|----------------------|
| Use CDC (Debezium) | New learning opportunity, decouples event emission from service | Outbox pattern (already known) |
| Configure Debezium connector | Standard CDC solution for PostgreSQL → Kafka | Manual CDC (rejected: reinventing wheel) |
| Update order status directly | Simple DB update triggers CDC automatically | Manual event creation (rejected: adds complexity) |

### Unknowns Resolved

- CDC latency: ~50-100ms is typical for Debezium
- Schema: Debezium can register Avro schemas in Schema Registry

---

## Phase 1: Design

### Source Code Structure

```text
FulfillmentService/
├── Models/
│   └── Order.cs              # ADD: FulfilledAt field
├── Repositories/
│   └── OrderRepository.cs   # MODIFY: update status + timestamp
└── Services/
    └── FulfillmentService.cs  # MODIFY: call repository
```

### Database Schema Changes

```sql
-- Add FulfilledAt to orders table
ALTER TABLE orders ADD COLUMN fulfilled_at TIMESTAMPTZ;
```

### New Infrastructure

```text
docker-compose/
├── debezium-connector/      # NEW: Debezium connector config
│   └── postgres-order-connector.json
```

### Data Flow (CDC)

```
1. OrderConsumer receives OrderPlaced event
2. FulfillmentService.ProcessOrder()
   a. Simulate fulfillment work
   b. Update order status to FULFILLED + set fulfilledAt in DB
3. Debezium connector (separate process)
   a. Watches PostgreSQL WAL (transaction log)
   b. Streams change events to Kafka topic: dbserver.public.orders
4. Downstream consumers
   a. Subscribe to CDC topic
   b. Filter for status='FULFILLED' events
```

### Key Implementation Points

- **Atomic by default**: DB commit is atomic; CDC captures WAL atomically
- **Idempotency**: Check if order already fulfilled before processing (FR-002)
- **No event publishing code**: Simply update the database row
- **Debezium config**: Postgres connector with Kafka Connect

---

## Complexity Tracking

| Violation | Why Needed | Simpler Alternative Rejected Because |
|-----------|------------|---------------------------------------|
| New Debezium infrastructure | CDC requires Kafka Connect setup | Outbox (already known, no new infrastructure) |

---

## Deliverables

1. **Models/Order.cs** - Add FulfilledAt field + Status enum update
2. **Repositories/OrderRepository.cs** - Add update methods
3. **docker-compose/debezium-connector/** - New Debezium config for orders table
4. **CDC documentation** - Integration guide for project