# Schemas

Avro schema definitions and code generation for Kafka messages.

## Adding or Updating Schemas

1. **Create/modify schema** in `Avro/` directory (e.g., `orders.placed.avsc`)
2. **Generate C# classes** from schemas:
   ```bash
   cd SchemaGenerator
   dotnet run
   ```
   Generated classes will be in `Generated/` directory
3. **Register schemas** with Schema Registry:
   ```bash
   docker-compose -f docker-compose.kafka.yml -f docker-compose.yml up -d --build schema-register
   ```

## Schema Format

Schemas use [Avro JSON format](https://avro.apache.org/docs/current/specification/):

```json
{
  "type": "record",
  "name": "YourMessage",
  "namespace": "Schemas",
  "fields": [
    { "name": "fieldName", "type": "string" }
  ]
}
```

## Schema Versioning

**Wire-Level Versioning**: Schema Registry automatically manages schema versions and IDs. Consumers use the schema ID embedded in messages to deserialize correctly.

**Application-Level Versioning**: Define version constants in your service for each event type (e.g., `OrderPlaced = 1`). Increment versions when making business-significant changes:
- **Breaking changes** (removed/renamed fields, changed semantics): Increment version
- **Additive changes** (new optional fields): Consider incrementing for tracking, but Schema Registry handles compatibility
