# Research: Order Completion Event Dispatch

## Decision: Event-Driven Architecture Pattern

**Chosen**: Kafka Pub/Sub with At-Least-Once Semantics

### Rationale

1. **Existing Infrastructure**: The codebase already uses Kafka with Confluent.Kafka client. Reusing this infrastructure minimizes new dependencies.

2. **Schema Registry**: Avro schemas are already generated and registered via existing tools. The OrderFulfilled event can follow the same pattern as OrderPlaced.

3. **Reliability Requirements**: The spec requires 99.9% delivery reliability. Kafka's built-in broker acknowledgment and consumer offset management provide this out of the box.

4. **Scalability**: Consumer group support allows horizontal scaling of OrderService consumers without duplicate processing.

### Alternatives Considered

| Alternative | Why Rejected |
|-------------|--------------|
| HTTP/Webhooks | No existing HTTP infrastructure between services, adds complexity |
| RabbitMQ | Not in current stack, would require new infrastructure |
| In-memory events | Doesn't support multiple consumers or durability |
| Database polling | Higher latency, less efficient than push-based Kafka |

## Decision: Event Schema Design

**Chosen**: Avro schema following OrderPlaced pattern with orderShortCode, customerId, fulfillmentTimestamp, and metadata

### Rationale

1. **Consistency**: OrderPlaced already uses Avro with orderShortCode and customerId. OrderFulfilled should match this pattern.

2. **Traceability**: Including metadata (correlationId, timestamp) supports debugging and tracing as required by FR-009.

3. **Evolution**: Avro supports backward-compatible schema evolution (adding optional fields).

## Decision: Consumer Implementation Pattern

**Chosen**: Background worker (IBackgroundService) following existing OrderConsumer pattern in FulfillmentService

### Rationale

1. **Existing Pattern**: FulfillmentService already implements a Kafka consumer as BackgroundService. Same pattern should be used in OrderService for consistency.

2. **Outbox Integration**: OrderService already has OutboxRepository. The consumer should write to the outbox for reliable status updates.

3. **Error Handling**: Confluent.Kafka supports retry and dead-letter mechanisms through error handling callbacks.

## Decision: Topic Naming

**Chosen**: `order.fulfilled`

### Rationale

1. **Convention**: Constitution states topics must follow `{domain}.{event-type}` format. Existing topic is `order.placed`.

2. **Clarity**: "fulfilled" clearly indicates the event meaning.
