# Kafka Example

## Getting Started

**Start the Kafka infrastructure:**
   ```bash
   docker compose up -d
   ```

**Access the Confluent Control Center:**
   Open http://localhost:9021 to view the Confluent Kafka control center

## Development

- To rebuild only producer: `docker-compose  -f docker-compose.kafka.yml -f docker-compose.yml up -d --build order-service`
- To rebuild only consumer: `docker-compose  -f docker-compose.kafka.yml -f docker-compose.yml up -d --build order-consumer`
- To rebuild only schema register: `docker-compose -f docker-compose.kafka.yml -f docker-compose.yml up -d --build schema-register`

## Schema Management

Schemas are **automatically registered** when the producer starts up. Simply add a new `.json` schema file to the `Schemas/` folder following the naming convention:

- **File name:** `{topic-name}.json` (e.g., `orders.json`)
- **Subject:** Automatically registered as `{topic-name}-value` (e.g., `orders-value`)

### Example
```
Schemas/
  orders.json  → Registers as subject "orders-value"
  users.json   → Registers as subject "users-value"
```
