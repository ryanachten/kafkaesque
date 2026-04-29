# Tasks: Flink Stream Analytics

**Input**: Design documents from `/specs/002-flink-stream-analytics/`
**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Organization**: Tasks are grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project initialization and basic structure

**Note**: Project was implemented as `flink-job-submitter/` (renamed from original `flink-analytics/` for multi-job support)

- [X] T001 Create flink-job-submitter/ project directory structure
- [X] T002 Initialize pom.xml with Flink 1.18.1, Kafka Connector 3.2.0-1.18, and Avro dependencies
- [X] T003 [P] Configure Maven build with shade plugin for fat JAR

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Core infrastructure that MUST be complete before ANY user story implementation

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

**Note**: Current implementation uses ProcessFunction with simple buffering (emits every 10 orders). True time-windowed aggregation is pending US1 implementation.

- [X] T004 Configure Kafka source for consuming from order.placed topic
- [X] T005 [P] Configure Avro deserialization using Schema Registry
- [X] T006 Configure Kafka sink for producing to order.analytics topic
- [X] T007 [P] Create WindowedMetric Avro schema in src/main/avro/
- [X] T008 Setup Flink StreamExecutionEnvironment configuration
- [X] T009 Implement MainAnalyticsJob.java entry point

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - Real-Time Metrics (Priority: P1) 🎯 MVP

**Goal**: Consume order events and emit windowed metrics (order count, revenue, average) every minute

**Independent Test**: Place orders via API and observe metrics in order.analytics topic within 2 minutes

**Checkpoint**: Foundation ready - user story implementation can now begin

### Implementation for User Story 1

- [ ] T012 [US1] Implement timestamp extractor for event-time processing
- [ ] T013 [US1] Implement 1-minute tumbling window with reduce function in src/main/java/com/kafkaesque/analytics/windows/
- [ ] T014 [US1] Implement metric aggregation (count, sum, avg) in src/main/java/com/kafkaesque/analytics/functions/
- [ ] T015 [US1] Configure watermark strategy with 60-second tolerance (depends on T012)
- [ ] T016 [US1] Replace ProcessFunction with proper window aggregation (depends on T013, T014)
- [ ] T017 [US1] Add logging for window firing and metric emission

**Checkpoint**: User Story 1 should be fully functional - metrics appear in output topic

---

## Phase 4: User Story 2 - Multiple Time Windows (Priority: P2)

**Goal**: Support 1-minute, 1-hour, and 24-hour tumbling windows

**Independent Test**: Verify all three window sizes produce metrics at their respective intervals

### Implementation for User Story 2

- [ ] T018 [P] [US2] Create 1-hour tumbling window pipeline in src/main/java/com/kafkaesque/analytics/windows/
- [ ] T019 [P] [US2] Create 24-hour tumbling window pipeline in src/main/java/com/kafkaesque/analytics/windows/
- [ ] T020 [US2] Implement window size differentiation in metric output (depends on T018, T019)
- [ ] T021 [US2] Add UTC midnight-to-midnight alignment for 24-hour window

**Checkpoint**: User Stories 1 and 2 should both work independently

---

## Phase 5: User Story 3 - Late Event Handling (Priority: P3)

**Goal**: Handle late-arriving events with watermarks, emit late events to side output

**Independent Test**: Inject events with timestamps in the past and verify inclusion/exclusion

### Implementation for User Story 3

- [ ] T022 [P] [US3] Configure side output for late events in src/main/java/com/kafkaesque/analytics/functions/
- [ ] T023 [US3] Implement late event logging and dead-letter handling
- [ ] T024 [US3] Add metrics for dropped vs included late events

**Checkpoint**: All user stories should now be independently functional

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Improvements that affect multiple user stories

- [ ] T025 [P] Add job restart checkpointing configuration (already enabled in MainAnalyticsJob)
- [ ] T026 Add metrics reporting (Flink's built-in metrics)
- [X] T027 Update docker-compose.yml to add Flink cluster (DONE)
- [X] T028 Create flink-job-submitter/Dockerfile (DONE)
- [ ] T029 Validate quickstart.md scenarios
- [ ] T030 [P] Add comment-based documentation for learning purposes

---

## Future Work

**Revenue Calculation**: The current `totalRevenue` field is actually summing item quantities (`totalUnits`). To calculate true revenue:

- Add `unitPrice` field to `OrderPlacedItem` in the Avro schema
- Join with Product service to get product prices
- Calculate: `SUM(item.count * item.unitPrice)` for each order
- Update `WindowedMetric` schema field name from `totalRevenue` to `totalUnits` (separate PR)

**Note**: This is tracked separately as it requires coordination with Product/Order services.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup - BLOCKS all user stories
- **User Stories (Phase 3-5)**: All depend on Foundational
  - Can proceed in parallel after Foundational complete
  - Or sequentially in priority order (P1 → P2 → P3)
- **Polish (Phase 6)**: Depends on all user stories complete

### User Story Dependencies

- **US1 (P1)**: Can start after Foundational - foundational for all stories
- **US2 (P2)**: Can start after Foundational - extends US1 window logic
- **US3 (P3)**: Can start after Foundational - enhances watermark config from US1

### Within Each User Story

- Models before functions
- Functions before pipeline configuration
- Story complete before moving to next

### Parallel Opportunities

- T002, T003 can run in parallel
- T004, T005, T006, T007, T008 can run in parallel
- T018, T019 can run in parallel
- T022, T024 can run in parallel
- Once Foundational complete, all user stories can start in parallel

---

## Parallel Example: User Story 1

```bash
# Launch model creation together:
Task: "Create OrderPlaced Java class in src/main/java/com/kafkaesque/analytics/model/"
Task: "Create WindowedMetric Java class in src/main/java/com/kafkaesque/analytics/model/"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: Verify metrics appear in order.analytics topic
5. Deploy/demo if ready

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add US1 → Test independently → Deploy/Demo (MVP!)
3. Add US2 → Test independently → Deploy/Demo
4. Add US3 → Test independently → Deploy/Demo

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational done:
   - Developer A: User Story 1
   - Developer B: User Story 2
   - Developer C: User Story 3
3. Stories complete and integrate independently

---

## PR Plan

| PR | Branch | Phase | Tasks | Description |
|----|--------|-------|-------|-------------|
| #1 | `feat/002-flink-stream-analytics` | Phases 1-2 | T001-T009 | Project structure, Kafka source/sink, Flink environment ✅ DONE |
| #2 | `feat/flink-us1-mvp` | Phase 3 | T012-T017 | 1-minute window, metrics - the working MVP |
| #3 | `feat/flink-us2-windows` | Phase 4 | T018-T021 | 1-hour and 24-hour windows |
| #4 | `feat/flink-us3-late-events` | Phase 5 | T022-T024 | Late event handling with watermarks |
| #5 | `feat/flink-polish` | Phase 6 | T025-T026, T029-T030 | Checkpoints, metrics, validation |

**Merge Order**: #1 → #2 → #3 → #4 → #5
**PR #1 is complete** - user stories can now begin.

---

## Task Summary

| Metric | Value |
|--------|-------|
| Total Tasks | 28 |
| Phase 1 (Setup) | 3 |
| Phase 2 (Foundational) | 6 |
| Phase 3 (US1 - MVP) | 6 |
| Phase 4 (US2) | 4 |
| Phase 5 (US3) | 3 |
| Phase 6 (Polish) | 6 |

| Status | Count |
|--------|-------|
| Completed | 11 |
| Pending | 17 |

| User Story | Task Count |
|-----------|-----------|
| US1 (P1) | 6 |
| US2 (P2) | 4 |
| US3 (P3) | 3 |

| Parallel Opportunities | 6 |
|-------------------|-----|

**Suggested MVP Scope**: User Story 1 only (T001-T017)