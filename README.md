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