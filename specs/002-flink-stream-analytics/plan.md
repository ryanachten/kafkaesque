# Implementation Plan: Flink Stream Analytics

**Branch**: `feat/002-flink-stream-analytics` | **Date**: April 26, 2026 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/002-flink-stream-analytics/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

Create a Flink stream processing job that consumes order events from the Kafka `order.placed` topic, calculates real-time metrics (order count, revenue, average order value) using time-windowed aggregations, and emits results to a downstream Kafka topic for visualization. This is a learning/exploration feature to understand Flink stream processing concepts.

## Technical Context

**Language/Version**: Java 17 (Flink's primary language)  
**Primary Dependencies**: Apache Flink 1.18+, Flink Kafka Connector, Avro  
**Storage**: Kafka topics (input and output)  
**Testing**: Flink testing utilities, JUnit 5  
**Target Platform**: Local development machine (Docker/docker-compose)  
**Project Type**: Stream processing job  
**Performance Goals**: Process 1000+ events/minute (from spec SC-004)  
**Constraints**: Learning feature - simplicity and understandability prioritized over optimization  
**Scale**: Small scale - single parallelism for learning

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Gate | Status | Notes |
|------|--------|-------|
| Learning-First: Feature prioritizes educational value | ✅ PASS | Real-time analytics is a practical, demonstrable use case |
| Simplicity: Favor simplest solution | ✅ PASS | Single Flink job, basic window aggregations |
| Understandability: Clear to newcomers | ✅ PASS | Will include comments explaining Flink concepts |
| Best Practices: Uses industry patterns | ✅ PASS | Demonstrates Kafka-Flink integration, watermarks |
| Progressive Complexity: Builds on existing knowledge | ✅ PASS | Leverages existing Kafka topics |

## Project Structure

### Documentation (this feature)

```text
specs/002-flink-stream-analytics/
├── plan.md              # This file (/speckit.plan command output)
├── research.md          # Phase 0 output (/speckit.plan command)
├── data-model.md        # Phase 1 output (/speckit.plan command)
├── quickstart.md        # Phase 1 output (/speckit.plan command)
├── contracts/           # Phase 1 output (/speckit.plan command)
└── tasks.md             # Phase 2 output (/speckit.tasks command - NOT created by /speckit.plan)
```

### Source Code (repository root)

```text
flink-analytics/
├── src/
│   └── main/
│       └── java/
│           └── com/
│               └── kafkaesque/
│                   └── analytics/
│                       ├── OrderEventDeserializer.java
│                       ├── WindowedMetricAggregator.java
│                       └── AnalyticsJob.java
├── pom.xml
└── Dockerfile

docker-compose.yml  # Updated to include Flink

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No violations - all gates pass.

---

## Phase 0: Research ✅ COMPLETE

Research completed in [research.md](./research.md)

### Research Outcomes

| Unknown | Decision | Key Insight |
|---------|----------|--------------|
| Flink Kafka Connector | Version 1.17.1 | Compatible with Flink 1.18 and Kafka 3.7 |
| Avro Integration | flink-avro-confluent-registry | Auto-fetches schema from Registry |
| Watermark Strategy | Bounded out-of-orderness (60s) | Matches spec SC-003 requirement |
| Window Type | Tumbling windows | Non-overlapping, simplest to understand |

---

## Phase 1: Design & Contracts ✅ COMPLETE

### Generated Artifacts

| Artifact | Path |
|----------|------|
| Data Model | [data-model.md](./data-model.md) |
| Quickstart | [quickstart.md](./quickstart.md) |
| Contract | [contracts/windowed-metric.md](./contracts/windowed-metric.md) |

### Design Decisions

| Decision | Rationale |
|----------|-----------|
| Tumbling windows | Non-overlapping, simplest to understand for learners |
| Avro output | Maintains consistency with existing event schemas |
| Single output topic | Aggregates all window sizes for simpler consumption |
| Decimal for money | Precise financial calculations |

---

## Phase 2: Implementation Ready

Ready for `/speckit.tasks` to generate implementation tasks.
