# Feature Specification: Flink Stream Analytics

**Feature Branch**: `feat/002-flink-stream-analytics`  
**Created**: April 26, 2026  
**Status**: Draft  
**Input**: User description: "I want to learn about using stream processing as part of this project using tooling such as Flink. What might be a good use case for exploring this? I was thinking perhaps real time analytics or something"

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Consume order events and visualize real-time metrics (Priority: P1)

As a system operator or analyst, I want to see real-time metrics about orders as they are placed so that I can monitor business health and spot trends early.

**Why this priority**: This is the primary learning objective - consuming events from Kafka and processing them through Flink to produce useful analytics.

**Independent Test**: Can be verified by placing orders via the API and observing aggregated metrics appear in the output sink within expected time bounds.

**Acceptance Scenarios**:

1. **Given** orders are being placed in the system, **When** the Flink job is running, **Then** order count metrics are produced every minute
2. **Given** a time window passes with no orders, **When** the Flink job processes the window, **Then** a metric with zero count is still produced
3. **Given** orders contain varying order values, **When** the Flink job processes them, **Then** aggregate revenue totals are correctly calculated

---

### User Story 2 - Process events with different time windows (Priority: P2)

As a system operator, I want to see analytics across different time windows (minute, hour, day) so that I can analyze both immediate trends and longer-term patterns.

**Why this priority**: Different window sizes teach different Flink concepts and provide more valuable business insights.

**Independent Test**: Can be verified by checking that multiple output streams exist with different aggregation intervals.

**Acceptance Scenarios**:

1. **Given** orders are arriving continuously, **When** viewed through a 1-minute window, **Then** I see order count per minute
2. **Given** orders are arriving continuously, **When** viewed through a 1-hour window, **Then** I see order count per hour with cumulative totals
3. **Given** orders are arriving continuously, **When** viewed through a 24-hour window, **Then** I see daily totals that reset at midnight

---

### User Story 3 - Handle late-arriving events with watermarks (Priority: P3)

As a system operator, I want the analytics to account for events that arrive slightly late due to network delays so that accuracy is maintained even with imperfect event timing.

**Why this priority**: This teaches critical Flink concept of watermarks for event-time processing in real-world scenarios.

**Independent Test**: Can be verified by injecting events with timestamps slightly in the past and confirming they are still included in aggregations.

**Acceptance Scenarios**:

1. **Given** an order event arrives 30 seconds late, **When** the watermark allows 60-second lateness, **Then** the event is included in the window aggregate
2. **Given** an order event arrives more than 60 seconds late, **When** processing, **Then** the event is dropped or assigned to a catch-up window

---

### Edge Cases

- What happens when the Kafka source topic is empty or has no messages?
- How does the system handle a Flink job restart - does it preserve state?
- What happens when the downstream sink is unavailable?
- How are events with malformed data handled?
- What happens when there's a clock skew between event producers and the Flink processor?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST consume order events from the `order.placed` Kafka topic
- **FR-002**: System MUST calculate order count per 1-minute time window using event timestamp
- **FR-003**: System MUST calculate total units per time window by summing item quantities (placeholder for future revenue calculation)
- **FR-004**: System MUST calculate average order value per time window
- **FR-005**: System MUST emit aggregated metrics to a downstream sink (topic or data store)
- **FR-006**: System MUST handle late-arriving events with configurable watermark tolerance
- **FR-007**: System MUST support multiple window sizes (1 minute, 1 hour, 24 hours)
- **FR-008**: System MUST provide metrics output in a format suitable for visualization

### Key Entities

- **Order Event**: Contains order ID, timestamp, customer ID, order total, and status
- **Windowed Metric**: Contains window start time, window end time, window size, order count, total units, and average order value (stored as strings for Confluent Avro compatibility)
- **Watermark**: Logical timestamp indicating the minimum event time for completeness

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can see order count metrics within 2 minutes of an order being placed
- **SC-002**: Revenue calculations are accurate to within 0.01 of actual totals
- **SC-003**: Late-arriving events within 60 seconds are correctly included in aggregations
- **SC-004**: The Flink job processes at least 1000 events per minute without lag
- **SC-005**: Aggregate metrics are available for both real-time viewing and historical analysis

## Assumptions

- The existing `order.placed` Kafka topic will have order events available for consumption
- The Avro schema for order events is already registered in Schema Registry
- The Flink cluster can be run locally for development and learning purposes
- Output metrics will be written to a Kafka topic for downstream consumption by visualization tools
- This is a learning/exploration feature - production SLAs are not required initially

## Future Enhancements

The following are identified as future enhancements to build towards:

- **Fulfillment Analytics**: Consume `order.placed` and `order.fulfilled` topics to calculate:
  - Pending vs fulfilled order counts per window
  - Fulfillment rate percentage
  - Average fulfillment latency (time between placed → fulfilled)
  
  This requires stream joining, statefulWindowing with late event updating, and designing for mutableWindow results rather than fire-and-forget aggregations.