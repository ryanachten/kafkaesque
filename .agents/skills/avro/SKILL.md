---
name: avro-schemas
description: Creates Avro schemas and generates corresponding C# classes for Kafka messages. Use this when working with Kafka message schemas, code generation, or Schema Registry.
---

# Avro Schemas for Kafka

This skill documents how to create Avro schemas and generate C# classes for use with Kafka messages and Schema Registry.

## Workflow

### 1. Create or Modify Schema

Create or edit the Avro schema file in the `Schemas/Avro/` directory. Use the `.avsc` extension.

Example schema (`orders.placed.avsc`):
```json
{
  "type": "record",
  "name": "OrderPlaced",
  "namespace": "Schemas",
  "fields": [
    { "name": "orderId", "type": "string" },
    { "name": "customerId", "type": "string" },
    { "name": "totalAmount", "type": "double" },
    { "name": "timestamp", "type": { "type": "long", "logicalType": "timestamp-millis" } }
  ]
}
```

### 2. Generate C# Classes

Run the SchemaGenerator to create C# classes from the Avro schemas:

```bash
cd SchemaGenerator
dotnet run
```

Generated classes will be output to the `Schemas/Generated/` directory.

### 3. Register with Schema Registry

Register schemas with the Schema Registry to enable versioning and compatibility checking:

```bash
docker-compose -f docker-compose.kafka.yml -f docker-compose.yml up -d --build schema-register
```

## Schema Format

Avro schemas use [JSON format](https://avro.apache.org/docs/current/specification/):

- **type**: Must be `"record"` for complex types
- **name**: The name of the message type (e.g., `OrderPlaced`)
- **namespace**: Namespace for the generated class (e.g., `Schemas`)
- **fields**: Array of field definitions with `name` and `type`

### Common Field Types

- Primitive: `string`, `int`, `long`, `double`, `boolean`, `bytes`
- Logical types: `{ "type": "long", "logicalType": "timestamp-millis" }`
- Arrays: `{ "type": "array", "items": "string" }`
- Optional fields: `[ "null", "string" ]`

## Schema Versioning

### Wire-Level Versioning

Schema Registry automatically manages schema versions and IDs. Consumers use the schema ID embedded in messages to deserialize correctly.

### Application-Level Versioning

Define version constants in your service for each event type (e.g., `OrderPlaced = 1`). Increment versions when making business-significant changes:

- **Breaking changes** (removed/renamed fields, changed semantics): Increment version
- **Additive changes** (new optional fields): Consider incrementing for tracking, but Schema Registry handles compatibility

## Best Practices

- Keep schema names descriptive and consistent (e.g., `OrderPlaced`, `PaymentProcessed`)
- Use meaningful field names that match your domain
- Add new fields as optional when extending schemas
- Avoid changing or removing existing fields to maintain backward compatibility
- Register schemas with Schema Registry before deploying consumers/producers