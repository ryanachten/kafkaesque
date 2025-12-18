namespace SchemaGenerator.Services;

public interface ISchemaGenerationService
{
    Task GenerateCodeFromSchemas(CancellationToken cancellationToken = default);
}
