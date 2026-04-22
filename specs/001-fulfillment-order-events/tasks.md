# Tasks: Fulfillment Order Events

**Feature**: 001-fulfillment-order-events | **Date**: April 22, 2026

## Implementation Strategy

**MVP Scope**: User Story 1 (Persist Fulfilled Order Date) - Tasks T001-T006
**Incremental Delivery**: Add User Story 2 (CDC Events) and User Story 3 (CDC Architecture)

---

## Phase 1: Setup

- [X] T001 [P] Create database migration for fulfilled_at column in FulfillmentService/Migrations/001_add_fulfilled_at.sql

---

## Phase 2: Foundational

- [X] T002 Modify Order model in FulfillmentService/Models/Order.cs - add Status and FulfilledAt properties
- [X] T003 [P] Create OrderStatus enum in FulfillmentService/Models/OrderStatus.cs

---

## Phase 3: User Story 1 - Persist Fulfilled Order Date

**Goal**: Update order record with fulfilled timestamp when fulfillment completes

**Independent Test**: Process order through fulfillment and verify fulfilled_at is populated in database

**Implementation Tasks**:

- [X] T004 [P] [US1] Create FulfillmentOrderRepository in FulfillmentService/Repositories/FulfillmentOrderRepository.cs with UpdateFulfilledStatus method
- [X] T005 [US1] Modify FulfillmentService/fulfillOrder in FulfillmentService/Services/FulfillmentService.cs to persist order status and timestamp via FulfillmentOrderRepository
- [X] T006 [US1] Implement idempotency check in FulfillmentOrderRepository to prevent overwriting existing fulfilled_at in FulfillmentService/Repositories/FulfillmentOrderRepository.cs (FR-002)

**Tests**:

- [ ] T007 [US1] Create integration test verifying fulfilled timestamp is persisted in FulfillmentService.Tests/OrderFulfillmentTests.cs
- [ ] T008 [US1] Create unit test verifying duplicate fulfillment requests are rejected in FulfillmentService.Tests/DuplicateFulfillmentTests.cs

---

## Phase 4: User Story 2 - Notify Downstream via CDC

**Goal**: Database changes trigger CDC events to Kafka for downstream consumers

**Independent Test**: Verify CDC events are published to Kafka topic when order status changes

**Implementation Tasks**:

- [X] T009 [P] [US2] Create Debezium connector configuration in docker-compose/debezium-config/postgres-orders-connector.json
- [X] T010 [US2] Add database update to FulfillmentOrderRepository in FulfillmentService/Repositories/FulfillmentOrderRepository.cs to set status to FULFILLED (triggers CDC)

**Tests**:

- [ ] T011 [US2] Create test verifying CDC event is emitted when order is fulfilled (integration test with Debezium)

---

## Phase 5: User Story 3 - CDC Architecture

**Goal**: Document CDC integration for project

**Implementation Tasks**:

- [X] T012 [US3] Create CDC integration documentation in docs/cdc-integration.md
- [ ] T013 [US3] Verify Debezium captures order table changes correctly

---

## Phase 6: Polish & Cross-Cutting Concerns

- [ ] T014 Run full test suite to verify all user stories work together
- [X] T015 Update Architecture section in README.md

---

## Dependencies

```
Phase 1 (Setup)
    └── Phase 2 (Foundational)
            ├── Phase 3: US1 (T004-T008)
            │       └── Phase 4: US2 (T009-T011)
            │               └── Phase 5: US3 (T012-T013)
            │                       └── Phase 6: Polish
            └── Phase 4: US2 can start after US1 (T004) creates repository
```

## Parallel Execution

- **T002**: Order model changes - no dependencies
- **T003**: OrderStatus enum - no dependencies
- **T001**: Database migration - no dependencies
- **T004**: FulfillmentOrderRepository - depends on T002, T003
- **T009**: Debezium config - no dependencies

---

## Summary

| Metric | Count |
|--------|-------|
| Total Tasks | 15 |
| User Story 1 | 5 |
| User Story 2 | 3 |
| User Story 3 | 2 |
| Setup/Polish | 5 |
| Parallelizable | 4 |

**Suggested MVP**: T001-T006 (User Story 1 only - Persist Fulfilled Order Date)