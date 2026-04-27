# Contract: WindowedMetric Event

## Overview

| Aspect | Detail |
|--------|--------|
| Event Name | WindowedMetric |
| Topic | order.analytics |
| Schema Registry | Schemas.WindowedMetric |
| Publisher | Flink Analytics Job |
| Subscriber | Visualization/Dashboard services |
| Delivery Semantics | At-least-once |

## Event Schema

```avro
{
  "type": "record",
  "name": "WindowedMetric",
  "namespace": "com.kafkaesque.analytics.model",
  "fields": [
    {
      "name": "windowStart",
      "type": "long",
      "doc": "Unix timestamp in milliseconds - start of time window"
    },
    {
      "name": "windowEnd",
      "type": "long",
      "doc": "Unix timestamp in milliseconds - end of time window"
    },
    {
      "name": "windowSize",
      "type": "string",
      "doc": "Duration of window: 1m, 1h, or 24h"
    },
    {
      "name": "orderCount",
      "type": "long",
      "doc": "Number of orders in this window"
    },
    {
      "name": "totalRevenue",
      "type": "string",
      "doc": "Sum of all order totals in this window"
    },
    {
      "name": "avgOrderValue",
      "type": "string",
      "doc": "Average order value in this window"
    },
    {
      "name": "processedAt",
      "type": "long",
      "doc": "Unix timestamp in milliseconds when metric was generated"
    }
  ]
}
```

**Note**: Using `string` type for monetary values (simplified from decimal for Confluent Avro compatibility). Events are emitted once per window when the window fires. No metadata headers required - all information is in the event body.

## Publisher Contract (Flink Analytics Job)

### Responsibilities

1. **Schema Registration**: Register WindowedMetric schema in Schema Registry before publishing
2. **Event Emission**: Emit to `order.analytics` topic when window fires
3. **Watermark Handling**: Drop late events or send to side output
4. **Restart Handling**: Maintain state for window aggregations across restarts

### Expected Behavior

| Scenario | Behavior |
|----------|----------|
| Window fires on time | Emit complete metric |
| No orders in window | Emit metric with zero counts |
| Event arrives late | Drop or emit to late-events side output |
| Downstream unavailable | Retry with Flink's built-in mechanisms |

## Subscriber Contract (Dashboard Service)

### Responsibilities

1. **Topic Subscription**: Subscribe to `order.analytics` via consumer group
2. **Deserialization**: Deserialize Avro using Schema Registry
3. **Visualization**: Display metrics in real-time dashboard
4. **Historical Analysis**: Store metrics for trend analysis

### Expected Behavior

| Scenario | Behavior |
|----------|----------|
| Valid event received | Display/update dashboard |
| Missing window (gap) | Alert/mark as missing |
| Late event received | Recalculate or flag |

## Consumer Group Configuration

| Setting | Value | Rationale |
|---------|-------|-----------|
| Group ID | analytics-dashboard-consumer | For scaling dashboard consumers |
| Auto Offset Reset | Latest | Only show current metrics |
| Enable Auto Commit | True | Simpler for dashboard |

## Event Flow

```
┌─────────────────────┐     order.placed      ┌─────────────────────┐
│  OrderService      │ ──────────���───────────► │  Flink Analytics   │
│                     │        (source)       │      Job           │
└─────────────────────┘                      │                    │
                                              │ 1. Consume events │
                                              │ 2. Assign        │
                                              │    timestamps     │
                                              │ 3. Apply         │
                                              │    watermarks    │
                                              │ 4. Tumbling      │
                                              │    window        │
                                              │ 5. Aggregate     │
                                              │ 6. Emit metric   │
                                              └─────────────────────┘
                                                     │
                                                     ▼
                                              ┌─────────────────────┐
                                              │  order.analytics   │ ─────────────────────► Dashboard
                                              └─────────────────────┘
```

## Version History

| Version | Changes |
|---------|---------|
| 1.0.0 | Initial version |