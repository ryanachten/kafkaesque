# CDC Integration Documentation

## Overview

This document describes the Change Data Capture (CDC) integration for the FulfillmentService. When orders are fulfilled, database changes are automatically captured and streamed to Kafka for downstream consumers.

## Architecture

```
┌─────────────────────┐     ┌──────────────────────┐     ┌─────────────────┐
│   OrderService     │────▶│  PostgreSQL WAL     │────▶│   Debezium      │
│   (Order Placed)  │     │  (orders table)     │     │   Connector     │
└─────────────────────┘     └──────────────────────┘     └────────┬────────┘
                                                                 │
                                                                 ▼
┌─────────────────────┐     ┌──────────────────────┐     ┌─────────────────┐
│  Downstream       │◀────│  Kafka Topic      │◀────│  Kafka         │
│  Consumers       │     │  dbserver.public  │     │  (broker)      │
└─────────────────────┘     └──────────────────────┘     └─────────────────┘
```

## Components

### Database Schema

The `orders` table tracks fulfillment status:

| Column | Type | Description |
|--------|------|-------------|
| order_short_code | VARCHAR | Unique order identifier |
| status | VARCHAR | Current order status (PENDING, FULFILLED, SHIPPED) |
| fulfilled_at | TIMESTAMPTZ | When fulfillment completed |

### FulfillmentService

The FulfillmentService processes orders and updates the database:

1. Receives `OrderPlaced` events from Kafka
2. Simulates fulfillment processing
3. Updates `orders` table with status and timestamp
4. Idempotency: rejects orders already fulfilled

### Debezium Connector

The Debezium PostgreSQL connector watches the WAL and streams changes to Kafka.

## Setup

### 1. Database

PostgreSQL must have logical replication enabled:

```sql
ALTER SYSTEM SET wal_level = logical;
SELECT pg_reload_conf();
-- Verify:
SHOW wal_level;
-- Expected: logical
```

Then run the migration to add fulfillment columns:

```sql
ALTER TABLE orders ADD COLUMN IF NOT EXISTS fulfilled_at TIMESTAMPTZ;
ALTER TABLE orders ADD COLUMN IF NOT EXISTS status VARCHAR(20) NOT NULL DEFAULT 'PENDING';
CREATE INDEX IF NOT EXISTS idx_orders_status ON orders(status);
CREATE INDEX IF NOT EXISTS idx_orders_fulfilled_at ON orders(fulfilled_at);
```

### 2. Kafka Connect with Debezium

The connector configuration is at `docker-compose/debezium-config/postgres-orders-connector.json`.

Register the connector after starting the services:

```bash
curl -i -X POST -H "Accept:application/json" \
  -H "Content-Type:application/json" \
  http://localhost:8083/connectors/ \
  -d @docker-compose/debezium-config/postgres-orders-connector.json
```

### 3. Verify

Consume CDC events from the topic:

```bash
docker-compose exec kafka kafka-console-consumer \
  --topic dbserver.public.orders \
  --from-beginning \
  --bootstrap-server broker:29092
```

## CDC Event Format

The connector uses the `ExtractNewRecordState` transform, which unwraps the Debezium envelope and produces the flattened payload (the `after` row only):

```json
{
  "order_short_code": "ABC1234567",
  "status": "FULFILLED",
  "fulfilled_at": "2026-04-22T10:30:00Z"
}
```

| Field | Description |
|-------|-------------|
| order_short_code | Unique order identifier |
| status | Order status (PENDING, FULFILLED, SHIPPED) |
| fulfilled_at | Timestamp when fulfillment completed |

## Configuration Reference

### FulfillmentService (appsettings.json)

```json
{
  "ConnectionStrings": {
    "FulfillmentDatabase": "Host=postgres;Port=5432;Database=orders;Username=postgres;Password=postgres"
  },
  "WorkerPool": {
    "WorkerCount": 5,
    "QueueCapacity": 100
  }
}
```

### Debezium Connector

Key configuration properties:

| Property | Description |
|---------|-------------|
| database.hostname | PostgreSQL host |
| database.port | PostgreSQL port (default 5432) |
| database.user | Database user |
| database.password | Database password |
| table.include.list | Tables to capture |
| topic.prefix | Prefix for Kafka topics |

## Troubleshooting

### No CDC events appearing

1. Verify PostgreSQL WAL is enabled:
   ```sql
   SHOW wal_level;
   ```
   Expected: `logical`

2. Check connector status:
   ```bash
   curl http://localhost:8083/connectors/postgres-orders-connector/status
   ```

3. Verify orders have been fulfilled:
   ```sql
   SELECT * FROM orders WHERE status = 'FULFILLED';
   ```

### Latency

CDC latency is typically 50-100ms. If higher:
- Check Kafka Connect worker load
- Verify network connectivity
- Review PostgreSQL WAL settings