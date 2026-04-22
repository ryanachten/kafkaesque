# Research: Fulfillment Order Events

## Decisions Made

### Decision 1: Use Change Data Capture (CDC)

**Rationale**: CDC is an industry-standard event-driven pattern that decouples event emission from application code. It provides atomicity by default (DB commit = event emitted) and is a valuable learning opportunity for the project.

**Alternatives Considered**:
- Outbox pattern: Already used in OrderService, less educational
- Direct Kafka publish: No atomicity guarantee
- CDC: Selected - new learning, proven at scale

---

### Decision 2: Debezium for CDC

**Rationale**: Debezium is the most mature open-source CDC solution, with well-documented PostgreSQL connector and Kafka Connect integration.

**Alternatives Considered**:
- Manual CDC: Rejected - reinventing the wheel
- AWS DMS: Rejected - vendor lock-in
- Debezium: Selected - open source, actively maintained

---

### Decision 3: Simple DB Update

**Rationale**: CDC captures changes automatically when we update the order table. No need for explicit event creation.

**Alternatives Considered**:
- Explicit event creation: Rejected - adds unnecessary code
- Trigger-based CDC: Rejected - Debezium handles cleanly
- Direct DB update: Selected - simple, CDC handles the rest

---

## Technical Findings

### Debezium + PostgreSQL

1. **WAL (Write-Ahead Log)**: PostgreSQL writes all changes to WAL first; Debezium reads from here
2. **Logical Decoding**: Uses `wal2json` or `pgoutput` plugin
3. **Topic naming**: `dbserver.{database}.{schema}.{table}` → `dbserver.public.orders`

### CDC Event Structure

```json
{
  "before": null,
  "after": { "order_short_code": "ABC123", "status": "FULFILLED", ... },
  "op": "u",  // u=update, c=create, d=delete
  "ts_ms": 1234567890
}
```

### Downstream Consumer Pattern

- Subscribe to `dbserver.public.orders`
- Filter for `op: "u"` and `after.status: "FULFILLED"`
- No schema registration needed - Debezium handles it