namespace SchemaManager.Services.SchemaGeneration;

public interface ISchemaGenerationService
{
    Task GenerateCodeFromSchemas(CancellationToken cancellationToken = default);
}

