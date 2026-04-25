# Data Model: Flink Stream Analytics

## Input Entity: OrderPlaced

Source: consumed from Kafka topic `order.placed`

| Field | Type | Description |
|-------|------|-------------|
| orderId | UUID | Unique order identifier |
| customerId | UUID | Customer who placed the order |
| timestamp | Instant | When the order was placed (event time) |
| total | Decimal | Order total amount |
| status | String | Order status (e.g., "placed") |
| items | Array | List of order items |

## Output Entity: WindowedMetric

Sink: emitted to Kafka topic `order.analytics`

| Field | Type | Description |
|-------|------|-------------|
| windowStart | Instant | Start of the time window |
| windowEnd | Instant | End of the time window |
| windowSize | String | Duration (1m, 1h, 24h) |
| orderCount | Long | Number of orders in window |
| totalRevenue | Decimal | Sum of all order totals |
| avgOrderValue | Decimal | Average order value |
| processedAt | Instant | When the metric was generated |

## Key Relationships

- **Input → Output**: One input event (OrderPlaced) contributes to exactly one output metric per window
- **Window Timing**: Based on event timestamp, not processing time
- **Late Events**: Events arriving after watermark are either dropped or sent to side output

## Validation Rules

- Order ID must be non-null
- Timestamp must be valid ISO-8601
- Total must be >= 0
- Window boundaries must align to minute/hour/day boundaries