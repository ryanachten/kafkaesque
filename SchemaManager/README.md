# Schema Manager

A short-lived console application that registers JSON schemas with the Kafka Schema Registry.

## Purpose

This application:
1. Reads all `.json` schema files from the `Schemas` directory
2. Registers them with the Kafka Schema Registry using the REST API
3. Exits after completion

## Usage

### Development
```bash
dotnet run --project SchemaManager
```

### Production
```bash
dotnet build -c Release
dotnet SchemaManager/bin/Release/net9.0/SchemaManager.dll
```

## Configuration

Configure the Schema Registry URL in `appsettings.json` or `appsettings.Development.json`:

```json
{
  "Kafka": {
    "BootstrapServers": "localhost:9092",
    "SchemaRegistryUrl": "http://localhost:8081"
  }
}
```

## Schema Naming Convention

Schemas follow the convention: `{topic-name}.json` → subject `{topic-name}-value`

For example:
- `orders.json` → subject `orders-value`
- `products.json` → subject `products-value`

## Exit Codes

- `0`: Success - all schemas registered
- `1`: Failure - error occurred during registration

## Notes

- If a schema already exists in the registry, it will be skipped (not an error)
- The application will fail fast if the Schema Registry is unavailable
- All schemas in the `Schemas` directory are automatically copied to the output directory during build
