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
cd flink-job-submitter
mvn clean package
```

### 3. Submit to Flink

```bash
docker-compose exec flink-jobmanager flink run \
  /opt/flink-jobs/flink-analytics.jar
```

### 4. Verify Output

Consume from the output topic to see metrics:

```bash
docker-compose exec broker kafka-console-consumer \
  --topic order.analytics \
  --from-beginning \
  --bootstrap-server localhost:9092
```

## Understanding the Output

Each minute, you should see metrics like:

```json
{
  "windowStart": 1745736000000,
  "windowEnd": 1745736060000,
  "windowSize": "1m",
  "orderCount": 42,
  "totalRevenue": "1234.56",
  "avgOrderValue": "29.39",
  "processedAt": 1745736065000
}
```

## Stopping the Job

```bash
docker-compose exec flink-jobmanager flink list
docker-compose exec flink-jobmanager flink cancel <job-id>
```