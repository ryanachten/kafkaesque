# Kafka Example

## Getting Started

1. **Start the Kafka infrastructure:**
   ```powershell
   docker-compose up -d
   ```

2. **Run the producer:**
   ```powershell
   dotnet run --project KafkaProducer/KafkaProducer.csproj
   ```
   
   Schemas are automatically registered on application startup from the `Schemas/` folder.

3. **Access the Confluent Control Center:**
   Open http://localhost:9021 to view the Confluent Kafka control center

## Development

- To rebuild only producer: `docker-compose up -d --build kafkaproducer`
- To rebuild only consumer: `docker-compose up -d --build kafkaconsumer`
- To rebuild only schema manager: `docker-compose -f docker-compose.kafka.yml -f docker-compose.yml up -d --build schemamanager`

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
