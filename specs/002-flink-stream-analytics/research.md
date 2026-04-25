# Research: Flink Stream Analytics

## Decision: Use Flink with Kafka Connector

**Rationale**: Flink is the industry-standard stream processing framework. It integrates natively with Kafka, supports event-time processing with watermarks, and provides rich windowed aggregation semantics.

## Alternatives Considered

- **Kafka Streams**: Simpler but lacks advanced windowing and state management features
- **Spark Structured Streaming**: More complex setup, less suitable for low-latency needs

---

## Research Findings

### 1. Flink Kafka Connector Compatibility

**Decision**: Use Flink Kafka Connector version 1.17.1 (compatible with both Flink 1.18 and Kafka 3.7)

**Details**:
- Flink 1.18 pairs with kafka-connector 1.17.1
- Kafka 3.7 is backward compatible with connector versions from Kafka 2.x
- Source configuration uses `KafkaSourceBuilder` with bootstrap servers, topic, and group ID

**Alternatives considered**: Using Flink 1.17.x with connector 1.17.0 (older but more stable)

### 2. Avro Integration with Schema Registry

**Decision**: Use `flink-avro-confluent-registry` connector for Avro deserialization

**Details**:
- Requires Confluent Maven repository
- Dependency: `flink-avro-confluent-registry` version 2.2.0 (for Flink 1.18)
- Uses `ConfluentRegistryAvroDeserializationSchema.forSpecific()` for typed events
- Automatically fetches schema from Schema Registry based on schema ID in message header

**Code Example**:
```java
KafkaSource<OrderPlaced> source = KafkaSource.<OrderPlaced>builder()
    .setBootstrapServers("localhost:9092")
    .setTopics("order.placed")
    .setGroupId("flink-analytics-group")
    .setStartingOffsets(OffsetsInitializer.earliest())
    .setValueOnlyDeserializer(
        ConfluentRegistryAvroDeserializationSchema.forSpecific(
            OrderPlaced.class,
            "http://localhost:8081"
        )
    )
    .build();
```

### 3. Watermark Strategies

**Decision**: Use bounded out-of-orderness watermark strategy with 60-second tolerance (matches spec SC-003)

**Details**:
- Watermark = max observed timestamp - allowed lateness
- Events arriving after watermark are considered late
- Can be dropped or processed in a side output for analysis

**Configuration**:
```java
WatermarkStrategy<OrderPlaced> watermarkStrategy = WatermarkStrategy
    .<OrderPlaced>forBoundedOutOfOrderness(Duration.ofSeconds(60))
    .withTimestampAssigner((event, timestamp) -> event.getTimestamp());
```

### 4. Window Types

**Decision**: Use tumbling windows for all three window sizes (1 min, 1 hour, 24 hours)

**Rationale**:
- Non-overlapping, fixed-size windows are simplest to understand
- Each event belongs to exactly one window (easier to debug)
- Matches spec requirement for periodic metrics

**Implementation**:
```java
DataStream<WindowedMetric> minuteWindowed = orders
    .keyBy(OrderPlaced::getCustomerId)
    .window(TumblingEventTimeWindows.of(Time.minutes(1)))
    .reduce(/* aggregation function */);
```

---

## Implementation Notes for Learning

1. Start with 1-minute tumbling window - simplest to verify
2. Use single parallelism for initial development (easier to understand)
3. Output to a separate Kafka topic for downstream visualization
4. Include logging to observe watermark progress and late events