# Implementation Plan: Order Completion Event Dispatch

**Branch**: `001-order-completion-event` | **Date**: 2026-04-23 | **Spec**: [spec.md](./spec.md)

## Summary

Implement an event-driven notification system where the FulfillmentService publishes an `OrderFulfilled` event to Kafka when order fulfillment completes, enabling the OrderService to update order status for customers. Uses Kafka for reliable event delivery with at-least-once semantics, following the existing outbox pattern and Avro schema conventions in the codebase.

## Technical Context

**Language/Version**: C# / .NET 9.0  
**Primary Dependencies**: Confluent.Kafka 2.5.3, Confluent.SchemaRegistry.Serdes.Avro 2.5.3, Dapper 2.1.35, Npgsql 8.0.5  
**Storage**: PostgreSQL (existing), Kafka (event bus), Schema Registry (Avro schemas)  
**Testing**: xUnit (project uses this)  
**Target Platform**: Linux server  
**Project Type**: Web service / Background worker (microservices)  
**Performance Goals**: 1000 events/minute throughput, <100ms publishing latency, <5s end-to-end status update  
**Constraints**: At-least-once delivery semantics, consumer group coordination, schema evolution compatibility  
**Scale/Scope**: Multiple OrderService consumer instances, horizontal scalability required  

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Notes |
|-----------|--------|-------|
| I. Learning-First | PASS | Implementation will include explanatory comments and documentation |
| II. Simplicity | PASS | Using existing Kafka infrastructure, minimal new components |
| III. Understandability | PASS | Clear event flow, descriptive naming, following existing patterns |
| IV. Best Practices | PASS | Outbox pattern already exists, schema registry used, consumer groups configured |
| V. Progressive Complexity | PASS | Builds on existing OrderPlaced event pattern |

## Project Structure

### Documentation (this feature)

```
specs/001-order-completion-event/
├── plan.md              # This file
├── research.md          # Phase 0 output
├── data-model.md        # Phase 1 output
├── quickstart.md        # Phase 1 output
├── contracts/           # Phase 1 output
│   └── order-fulfilled-event.md
└── tasks.md            # Phase 2 output (/speckit.tasks)
```

### Source Code (repository root)

```
FulfillmentService/
├── Services/
│   └── OrderFulfilledProducer.cs    # NEW: Publishes OrderFulfilled events
├── Schemas/
│   └── (reuse Avro generation from existing)  # NEW: OrderFulfilled schema
└── appsettings.json

OrderService/
├── Services/
│   └── OrderFulfilledConsumer.cs   # NEW: Consumes OrderFulfilled events
├── Repositories/
│   └── (existing: OutboxRepository) # REUSE: For reliable status updates
├── Models/
│   └── (existing: Order)             # REUSE: Add FULFILLED status transition
└── Program.cs                        # MODIFY: Register consumer

Schemas/
├── SchemaGenerator/                   # REUSE: Generate OrderFulfilled Avro schema
└── SchemaRegister/                   # REUSE: Register schema in registry
```

**Structure Decision**: Two new components in existing services (FulfillmentService producer, OrderService consumer) following established patterns. No new projects required.

## Complexity Tracking

> Not applicable - no Constitution violations detected.

## Phase 0: Research

*See research.md for detailed findings*

## Phase 1: Design

*See data-model.md, contracts/order-fulfilled-event.md, and quickstart.md for detailed design*
