# Tasks: Order Completion Event Dispatch

**Input**: Design documents from `specs/001-order-completion-event/`
**Prerequisites**: plan.md (required), spec.md (required for user stories), research.md, data-model.md, contracts/

**Tests**: Not explicitly requested in spec - omit test tasks per instructions

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

---

## Phase 1: Setup (Kafka Infrastructure)

**Purpose**: Configure Kafka topics and register schemas

- [ ] T001 [P] Create `order.fulfilled` topic in Kafka brokers
- [x] T002 [P] Register OrderFulfilled schema in Schema Registry

---

## Phase 2: Foundational (Shared Components)

**Purpose**: Core infrastructure that MUST be complete before ANY user story can be implemented

- [x] T003 Add OrderFulfilled topic constant in Common/Constants.cs
- [x] T004 Generate Avro schema for OrderFulfilled event in Schemas/Generated/Schemas/
- [x] T005 Generate EventMetadata schema class in Schemas/Generated/Schemas/

**Checkpoint**: Foundation ready - user story implementation can now begin

---

## Phase 3: User Story 1 - Order Fulfillment Completion Notification (Priority: P1) 🎯 MVP

**Goal**: Enable FulfillmentService to publish OrderFulfilled event when order fulfillment completes, and OrderService to consume and update order status

**Independent Test**: Publish an OrderFulfilled event and verify order status updates to FULFILLED in database

### Implementation for User Story 1

- [x] T006 [P] [US1] Create OrderFulfilled Avro generated class in Schemas/Generated/Schemas/OrderFulfilled.cs
- [x] T007 [P] [US1] Create EventMetadata Avro generated class in Schemas/Generated/Schemas/EventMetadata.cs
- [x] T008 [US1] Implement IOrderFulfilledProducer interface in FulfillmentService/Services/IOrderFulfilledProducer.cs
- [x] T009 [US1] Implement OrderFulfilledProducer in FulfillmentService/Services/OrderFulfilledProducer.cs (depends on T008)
- [x] T010 [US1] Integrate OrderFulfilledProducer into FulfillmentService/Program.cs (depends on T009)
- [x] T011 [US1] Implement IOrderFulfilledConsumer interface in OrderService/Services/IOrderFulfilledConsumer.cs
- [x] T012 [US1] Implement OrderFulfilledConsumer in OrderService/Services/OrderFulfilledConsumer.cs
- [x] T013 [US1] Register OrderFulfilledConsumer as hosted service in OrderService/Program.cs (depends on T012)
- [x] T014 [US1] Add status update logic to OrderFulfilledConsumer for mapping FULFILLED status

**Checkpoint**: At this point, User Story 1 should be fully functional and testable independently

---

## Phase 4: User Story 2 - Fulfillment Event Delivery Reliability (Priority: P2)

**Goal**: Ensure events are reliably delivered with at-least-once semantics, handling transient failures

**Independent Test**: Introduce broker failures and verify events are eventually delivered

### Implementation for User Story 2

- [x] T015 [P] [US2] Add retry logic with exponential backoff in OrderFulfilledProducer
- [x] T016 [P] [US2] Add error handling callbacks in OrderFulfilledProducer
- [x] T017 [US2] Add dead-letter handling in OrderFulfilledConsumer for failed events (depends on T012)
- [x] T018 [US2] Configure manual offset commit in OrderFulfilledConsumer (depends on T012)

**Checkpoint**: At this point, User Stories 1 AND 2 should both work reliably

### Common DLQ Infrastructure (Shared)

- [x] T025 [P] [US2] Create DeadLetterExtensions in Common for shared DLQ header handling
- [x] T026 [P] [US2] Add per-service DLQ topic constants in Constants.cs
- [x] T027 [P] [US2] Add DLQ support to FulfillmentService OrderConsumer
- [x] T028 [US2] Refactor OrderService consumer to use shared DLQ pattern

---

## Phase 5: User Story 3 - Event Consumer Scalability (Priority: P3)

**Goal**: Support horizontal scaling of consumers without duplicate processing

**Independent Test**: Run multiple consumer instances and verify exactly-once processing

### Implementation for User Story 3

- [ ] T019 [P] [US3] Configure consumer group in OrderFulfilledConsumer
- [ ] T020 [US3] Add idempotency check in OrderFulfilledConsumer to skip already-processed events (depends on T012)

**Checkpoint**: All user stories should now be independently functional

---

## Phase 6: Polish & Cross-Cutting Concerns

- [ ] T021 [P] Run quickstart.md validation steps
- [ ] T022 [P] Verify end-to-end latency meets SC-001 (< 5 seconds)
- [ ] T023 Verify delivery reliability meets SC-002 (99.9%)
- [ ] T024 Verify throughput meets SC-003 (1000 events/min)

---

## PR Strategy

Recommended to PR incrementally by phase for focused reviews:

| PR | Phase(s) | Scope | Deliverable |
|----|----------|-------|-------------|
| #1 | 1-2 | Setup + Foundational | Infrastructure ready |
| #2 | 3 | **US1 (MVP)** | Core event flow works end-to-end |
| #3 | 4 | US2 | Retry + reliability added |
| #4 | 5 | US3 | Scalability complete |

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - BLOCKS all user stories
- **User Stories (Phase 3+)**: All depend on Foundational phase completion
  - User stories can proceed in parallel after Phase 2 (if staffed)
  - Or sequentially in priority order (P1 → P2 → P3)
- **Polish (Final Phase)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: Can start after Foundational (Phase 2) - No dependencies on other stories
- **User Story 2 (P2)**: Can start after Foundational (Phase 2) - Builds on US1 producer/consumer but independently testable
- **User Story 3 (P3)**: Can start after Foundational (Phase 2) - Builds on US1 consumer but independently testable

### Within Each User Story

- Models before services
- Interfaces before implementations
- Producer before consumer integration
- Story complete before moving to next priority

### Parallel Opportunities

- Phase 1 tasks (T001, T002) can run in parallel
- Phase 2 tasks (T003-T005) can run in parallel
- Phase 3 parallel tasks (T006, T007) can run in parallel
- Phase 4 parallel tasks (T015, T016) can run in parallel
- Phase 5 parallel tasks (T019) - single task

---

## Parallel Example: User Story 1

```bash
# Launch all parallel tasks for User Story 1 together:
Task: "Create OrderFulfilled Avro generated class in Schemas/Generated/Schemas/OrderFulfilled.cs"
Task: "Create EventMetadata Avro generated class in Schemas/Generated/Schemas/EventMetadata.cs"
Task: "Implement IOrderFulfilledProducer interface in FulfillmentService/Services/IOrderFulfilledProducer.cs"
Task: "Implement IOrderFulfilledConsumer interface in OrderService/Services/IOrderFulfilledConsumer.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: Test User Story 1 independently
5. Deploy/demo if ready

### Incremental Delivery

1. Complete Setup + Foundational → Foundation ready
2. Add User Story 1 → Test independently → Deploy/Demo (MVP!)
3. Add User Story 2 → Test independently → Deploy/Demo
4. Add User Story 3 → Test independently → Deploy/Demo
5. Each story adds value without breaking previous stories

### Parallel Team Strategy

With multiple developers:

1. Team completes Setup + Foundational together
2. Once Foundational is done:
   - Developer A: User Story 1 (producer + consumer)
   - Developer B: User Story 2 (reliability)
   - Developer C: User Story 3 (scalability)
3. Stories complete and integrate independently

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- Avoid: vague tasks, same file conflicts, cross-story dependencies that break independence