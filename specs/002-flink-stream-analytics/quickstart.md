# Quickstart: Flink Stream Analytics

## Prerequisites

- Docker and Docker Compose
- Java 17+
- Maven 3.8+

## Running the Analytics Job

### 1. Start Infrastructure

```bash
./run.sh
```

This starts Kafka, Schema Registry, and adds the Flink service.

### 2. Build the Flink Job

```bash
cd flink-analytics
mvn clean package
```

### 3. Submit to Flink

```bash
docker-compose exec jobmanager flink run -c com.kafkaesque.analytics.AnalyticsJob \
  /analytics/target/analytics-job-1.0.jar
```

### 4. Verify Output

Consume from the output topic to see metrics:

```bash
docker-compose exec kafka kafka-console-consumer \
  --topic order.analytics \
  --from-beginning \
  --bootstrap-server localhost:9092
```

## Understanding the Output

Each minute, you should see metrics like:

```json
{
  "windowStart": "2026-04-26T10:00:00Z",
  "windowEnd": "2026-04-26T10:01:00Z",
  "windowSize": "1m",
  "orderCount": 42,
  "totalRevenue": 1234.56,
  "avgOrderValue": 29.39,
  "processedAt": "2026-04-26T10:01:05Z"
}
```

## Stopping the Job

```bash
docker-compose exec jobmanager flink list
docker-compose exec jobmanager flink cancel <job-id>
```