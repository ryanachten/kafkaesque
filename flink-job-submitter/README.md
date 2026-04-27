# Flink Job Submitter

Generic container for submitting Flink jobs to the cluster. Supports multiple job JARs via build args.

## Architecture Overview

```mermaid
graph TB
    subgraph Docker["Docker Compose Stack"]
        subgraph Flink["Flink Cluster"]
            JM[flink-job-manager<br/>Job Coordinator<br/>:8081]
            TM[flink-task-manager<br/>Processing<br/>Executor]
            FA[flink-job-submitter<br/>Job Submitter<br/>Runs & Exits]
            
            FA -->|"flink run -d"| JM
            JM <-->|"Scheduling"| TM
        end
        
        KB[Kafka Broker<br/>:29092]
        SR[Schema Registry<br/>:8081]
        
        FA -->|"order.placed"| KB
        KB -->|"order.analytics"| FA
        
        KB <-->|"Registry Lookups"| SR
    end
    
    OS[Order Service<br/>Producer] -->|"order.placed"| KB
```

## Container Roles

### flink-job-submitter
- **Role**: Generic job submitter (run once, then exit)
- **Responsibilities**:
  - Submits any Flink job JAR to the JobManager
  - Uses `FLINK_JOB_JAR` build arg to specify which JAR
  - Container exits after submission (this is expected)
  - Actual processing happens on the Flink cluster
- **Lifecycle**: Short-lived, analogous to a migration script

## Multi-Job Support

This container can submit multiple different Flink jobs. Build with different JAR names:

```bash
# Build analytics job
docker build --build-arg FLINK_JOB_JAR=flink-analytics.jar -t flink-job-submitter:analytics .

# Build reporting job
docker build --build-arg FLINK_JOB_JAR=flink-reporting.jar -t flink-job-submitter:reporting .
```

Or reference in docker-compose with multiple services:

```yaml
flink-analytics-submitter:
  build:
    context: ./flink-job-submitter
    args:
      - FLINK_JOB_JAR=flink-analytics.jar

flink-reporting-submitter:
  build:
    context: ./flink-job-submitter
    args:
      - FLINK_JOB_JAR=flink-reporting.jar
```

## Processing Pipeline

### Data Flow

```mermaid
sequenceDiagram
    participant OS as Order Service
    participant KB as Kafka Broker
    participant FA as Flink Analytics
    
    OS->>KB: POST /orders (order.placed)
    KB-->>FA: order.placed events
    FA->>FA: Buffer & Aggregate
    FA->>KB: order.analytics (metrics)
```

### Job Components

1. **KafkaSource** (`MainAnalyticsJob.java:89-95`)
   - Reads from `order.placed` topic
   - Uses Confluent Avro deserialization
   - Group ID: `flink-analytics-consumer-v2`

2. **ProcessFunction** (`MainAnalyticsJob.java:135-186`)
   - Buffer incoming orders
   - Emit metrics every 10 orders
   - Calculate: count, total items, average order value

3. **KafkaSink** (`MainAnalyticsJob.java:108-114`)
   - Writes to `order.analytics` topic
   - Uses Confluent Avro serialization

## Checkpointing & Fault Tolerance

Flink uses checkpointing for exactly-once processing:

- **Interval**: Every 60 seconds
- **What's checkpointed**:
  - Kafka consumer offset (resumes from last processed event)
  - Operator state (window buffers)
- **On failure**: Job restarts from last checkpoint

## Running Locally

### Build
```bash
cd flink-job-submitter
mvn clean package
```

### Run Job (via Docker)
```bash
docker-compose up -d flink-job-submitter
# Watch logs:
docker logs -f flink-job-submitter
```

### Build Specific Job
```bash
# Build analytics job (default)
docker build --build-arg FLINK_JOB_JAR=flink-analytics.jar -t flink-job-submitter:analytics ./flink-job-submitter
```

## Monitoring

- **Flink Web UI**: http://localhost:8084
  - Job status, metrics, checkpoints
- **Kafka topics**: http://localhost:9021 (Kafka Control Center)
- **Container logs**: `docker logs flink-task-manager`

## Configuration

Key settings in `application.properties`:

| Property | Description | Default |
|----------|-------------|---------|
| `kafka.bootstrap.servers` | Kafka broker address | `broker:29092` |
| `kafka.schema.registry.url` | Schema Registry URL | `http://schema-registry:8081` |
| `flink.source.topic` | Input topic | `order.placed` |
| `flink.sink.topic` | Output topic | `order.analytics` |
| `flink.checkpoint.interval` | Checkpoint frequency | `60000ms` |