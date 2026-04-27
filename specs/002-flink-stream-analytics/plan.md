# Implementation Plan: Flink Stream Analytics

**Branch**: `feat/002-flink-stream-analytics` | **Date**: April 26, 2026 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/002-flink-stream-analytics/spec.md`

**Note**: This template is filled in by the `/speckit.plan` command. See `.specify/templates/plan-template.md` for the execution workflow.

## Summary

Create a Flink stream processing job that consumes order events from the Kafka `order.placed` topic, calculates real-time metrics (order count, revenue, average order value) using time-windowed aggregations, and emits results to a downstream Kafka topic for visualization. This is a learning/exploration feature to understand Flink stream processing concepts.

## Technical Context

**Language/Version**: Java 17 (Flink's primary language)
**Primary Dependencies**: Apache Flink 1.18.1, Flink Kafka Connector 3.2.0-1.18, Flink Avro Confluent Registry
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
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   └── windowed-metric.md
└── tasks.md             # Phase 2 output
```

### Source Code (repository root)

```text
flink-job-submitter/              # Renamed from flink-analytics for multi-job support
├── src/
│   └── main/
│       ├── avro/
│       │   ├── order-placed.avsc         # Input schema (matches Registry's Schemas.OrderPlaced)
│       │   └── windowed-metric.avsc      # Output schema (com.kafkaesque.analytics.model.WindowedMetric)
│       ├── java/
│       │   └── com/
│       │       └── kafkaesque/
│       │           └── analytics/
│       │               └── MainAnalyticsJob.java   # Entry point
│       └── resources/
│           └── application.properties     # Kafka, Flink, Schema Registry config
├── pom.xml
├── Dockerfile
└── README.md                          # Architecture docs

flink-conf/                           # Flink cluster configuration
├── analytics.yaml                     # JobManager config
└── taskmanager.yaml                   # TaskManager config (4 slots)

docker-compose.yml                    # Flink cluster services
```

## Complexity Tracking

> **Fill ONLY if Constitution Check has violations that must be justified**

No violations - all gates pass.

---

## Phase 0: Research ✅ COMPLETE

Research completed in [research.md](./research.md)

### Research Outcomes

| Unknown | Decision | Key Insight |
|---------|----------|--------------|
| Flink Kafka Connector | Version 3.2.0-1.18 | Compatible with Flink 1.18.1 and Kafka 3.7 |
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
| String for money | Simplified from decimal for Confluent Avro compatibility |
| Multi-job submitter | Generic container supports multiple Flink jobs |

---

## Phase 2: Implementation ✅ COMPLETE

### PR #1: Feat/flink-setup - Phases 1-2

| Task | Description | Status |
|------|-------------|--------|
| T001 | Project directory structure | ✅ Done |
| T002 | pom.xml with Flink 1.18, Kafka, Avro | ✅ Done |
| T003 | Maven shade plugin for fat JAR | ✅ Done |
| T004 | Kafka source (order.placed) | ✅ Done |
| T005 | Avro deserialization via Schema Registry | ✅ Done |
| T006 | Kafka sink (order.analytics) | ✅ Done |
| T007 | WindowedMetric Avro schema | ✅ Done |
| T008 | StreamExecutionEnvironment config | ✅ Done |
| T009 | MainAnalyticsJob entry point | ✅ Done |

### Files Delivered in PR #1

| File | Purpose |
|------|---------|
| `flink-job-submitter/pom.xml` | Dependencies: Flink 1.18.1, Kafka 3.2.0-1.18, Avro, Confluent Registry |
| `flink-job-submitter/src/main/avro/*.avsc` | Avro schemas for input/output |
| `flink-job-submitter/src/main/java/.../MainAnalyticsJob.java` | Job entry point with Kafka source/sink |
| `flink-job-submitter/src/main/resources/application.properties` | Configuration |
| `flink-job-submitter/Dockerfile` | Multi-job submitter image |
| `flink-job-submitter/README.md` | Architecture documentation |
| `flink-conf/analytics.yaml` | JobManager config |
| `flink-conf/taskmanager.yaml` | TaskManager config (4 slots) |
| `docker-compose.yml` | Flink cluster (jobmanager, taskmanager, submitter) |

---

## Phase 3-6: User Stories & Polish

See [tasks.md](./tasks.md) for detailed implementation tasks.

### PR #2: User Story 1 (MVP) - Real-Time Metrics
- Implement proper 1-minute tumbling window with watermark strategy
- Calculate order count, revenue, average order value
- Emit WindowedMetric events to order.analytics topic

### PR #3-5: User Stories 2-3 & Polish
- Additional window sizes (1h, 24h)
- Late event handling with side outputs
- Docker, checkpoints, documentation

---

## Implementation Status

```text
┌─────────────────────────────────────────────────────────────┐
│ Phase 1: Setup                                    [COMPLETE] │
│ Phase 2: Foundational                             [COMPLETE] │
│ Phase 3: User Story 1 (MVP)                       [PENDING]  │
│ Phase 4: User Story 2 (Multiple Windows)          [PENDING]  │
│ Phase 5: User Story 3 (Late Events)               [PENDING]  │
│ Phase 6: Polish & Cross-Cutting                  [PENDING]  │
└─────────────────────────────────────────────────────────────┘
```