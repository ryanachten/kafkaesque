# Contract: Order CDC Event

## Overview

Order changes are captured via Debezium CDC with `ExtractNewRecordState` transform and streamed to Kafka.

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
    "key.converter": "org.apache.kafka.connect.json.JsonConverter",
    "key.converter.schemas.enable": "false",
    "value.converter": "org.apache.kafka.connect.json.JsonConverter",
    "value.converter.schemas.enable": "false",
    "column.include.list": "public.orders:order_short_code,status,fulfilled_at",
    "transforms": "unwrap",
    "transforms.unwrap.type": "io.debezium.transforms.ExtractNewRecordState",
    "transforms.unwrap.drop.tombstones": "false"
  }
}
```

## Event Payload (Flattened)

The `ExtractNewRecordState` transform unwraps the Debezium envelope, producing the `after` row only:

```json
{
  "order_short_code": "ABC123DEF",
  "status": "FULFILLED",
  "fulfilled_at": "2026-04-22T10:30:00Z"
}
```

## Example Events

### Order Fulfilled (update)

```json
{
  "order_short_code": "ABC123DEF",
  "status": "FULFILLED",
  "fulfilled_at": "2026-04-22T10:30:00Z"
}
```

## Consumer Expectations

1. Subscribe to `dbserver.public.orders`
2. Filter for `status: "FULFILLED"` (fulfillment events only)
3. Handle schema evolution (Debezium handles compatibility)