<!--
Sync Impact Report:
- Version change: N/A → 1.0.0 (Initial constitution)
- Modified principles: N/A (all new)
- Added sections: Development Philosophy, Architecture Standards
- Removed sections: None
- Templates requiring updates: ✅ N/A (no constitution-specific references in templates)
- Follow-up TODOs: None
-->

# Kafkaesque Constitution

## Core Principles

### I. Learning-First

Every feature, example, and documentation MUST prioritize educational value. Code examples must include explanatory comments and link to relevant concepts. The project exists to help developers learn Kafka and event-driven architecture through hands-on implementation.

### II. Simplicity

Implementation MUST favor the simplest solution that achieves the learning objective. Avoid premature abstraction or over-engineering. Introduce complexity only when required to demonstrate advanced patterns or solve real problems.

### III. Understandability

All code and documentation MUST be clear and accessible to developers new to Kafka. Variable names, function names, and class names MUST be descriptive. Complex logic MUST include inline comments explaining the "why" not just the "what".

### IV. Best Practices Adoption

Examples and implementations MUST demonstrate industry-standard patterns for Kafka and event-driven systems. This includes the outbox pattern for reliable publishing, schema evolution handling, proper consumer group configuration, and appropriate retry/dead-letter strategies. Justify any deviation from established best practices in code comments.

### V. Progressive Complexity

Learning resources MUST be organized to introduce concepts incrementally. Start with simple producers/consumers, then advance to schema registry, exactly-once semantics, and sophisticated patterns. Each stage MUST build on previous knowledge.

## Development Philosophy

The project follows a hands-on, example-driven approach. All concepts MUST be demonstrated through working code that learners can run, modify, and experiment with. Theoretical explanations MUST be accompanied by practical implementations.

### Workflow Requirements

- All new features MUST include a working example that demonstrates the concept
- Documentation MUST explain both how to use something and why it works that way
- Breaking changes in examples MUST be clearly marked with migration guidance
- Each component MUST have a corresponding explainer or tutorial

## Architecture Standards

The project demonstrates event-driven architecture using C# and .NET, but the patterns are transferable. All implementations MUST follow these structural rules:

### Service Structure

- Order Service: REST API with outbox pattern for reliable event publishing
- Consumer Services: Background workers that process events independently
- Infrastructure: Schema Registry, Kafka broker, and topic management tools

### Data Patterns

- Avro schemas MUST be used for all event types with clear evolution policies
- Topics MUST follow naming conventions: `{domain}.{event-type}`
- Consumer groups MUST be properly configured for scalability and durability

**Version**: 1.0.0 | **Ratified**: 2026-04-22 | **Last Amended**: 2026-04-22