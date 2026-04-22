# Quickstart: Fulfillment Order Events with CDC

## Overview

This feature adds event-driven fulfillment using Change Data Capture (CDC):
1. Persists fulfilled timestamp when order processing completes
2. Debezium captures order status changes and streams to Kafka

## Prerequisites

- PostgreSQL database with `orders` table
- Kafka Connect with Debezium connector
- .NET 8 SDK

## Build & Run

```bash
# Build the service
dotnet build FulfillmentService/FulfillmentService.csproj

# Run the service
dotnet run --project FulfillmentService/FulfillmentService.csproj
```

## Start Debezium Connector

```bash
# Register Debezium PostgreSQL connector
curl -i -X POST -H "Accept:application/json" \
  -H "Content-Type:application/json" \
  http://localhost:8083/connectors/ \
  -d @docker-compose/debezium-config/postgres-orders-connector.json
```

## Testing

```bash
# Run unit tests
dotnet test FulfillmentService.Tests/
```

## Configuration

```json
{
  "Database": {
    "Host": "localhost",
    "Port": 5432,
    "Name": "orders"
  },
  "WorkerPool": {
    "WorkerCount": 5
  }
}
```

## Database Changes

```sql
-- Add fulfillment timestamp to orders table
ALTER TABLE orders ADD COLUMN fulfilled_at TIMESTAMPTZ;
```

## Verification

1. Place an order via OrderService
2. Verify FulfillmentService processes it
3. Check database: `SELECT * FROM orders WHERE status = 'FULFILLED'`
4. Consume CDC topic: `docker-compose exec kafka kafka-console-consumer --topic dbserver.public.orders --from-beginning`