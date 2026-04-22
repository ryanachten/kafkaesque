# Contract: Order CDC Event

## Overview

Order changes are captured via Debezium CDC and streamed to Kafka.

## Topic

`dbserver.public.orders`

## Debezium Configuration

```json
{
  "name": "postgres-orders-connector",
  "config": {
    "connector.class": "io.debezium.connector.postgresql.PostgresConnector",
    "database.hostname": "localhost",
    "database.port": "5432",
    "database.user": "postgres",
    "database.password": "postgres",
    "database.dbname": "orders",
    "table.include.list": "public.orders",
    "plugin.name": "pgoutput",
    "topic.prefix": "dbserver",
    "schema.enable": "true"
  }
}
```

## Event Schema

```json
{
  "type": "record",
  "name": "Envelope",
  "namespace": "dbserver.public.orders",
  "fields": [
    { "name": "before", "type": ["null", { "type": "record", "fields": [...] }], "default": null },
    { "name": "after", "type": ["null", { "type": "record", "fields": [...] }], "default": null },
    { "name": "op", "type": "string" },
    { "name": "ts_ms", "type": "long" }
  ]
}
```

## Example Events

### Order Fulfilled (update)

```json
{
  "before": {
    "order_short_code": "ABC123DEF",
    "status": "PENDING",
    "fulfilled_at": null
  },
  "after": {
    "order_short_code": "ABC123DEF",
    "status": "FULFILLED",
    "fulfilled_at": "2026-04-22T10:30:00Z"
  },
  "op": "u",
  "ts_ms": 1713781800000
}
```

## Consumer Expectations

1. Subscribe to `dbserver.public.orders`
2. Filter for `op: "u"` (updates only)
3. Filter for `after.status: "FULFILLED"`
4. Handle schema evolution (Debezium handles compatibility)