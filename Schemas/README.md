# Schemas

Avro schema definitions and code generation for Kafka messages.

## Adding or Updating Schemas

1. **Create/modify schema** in `Avro/` directory (e.g., `orders.avsc`)
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
