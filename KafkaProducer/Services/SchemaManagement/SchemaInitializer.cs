using Common;
using System.Text;
using System.Text.Json;

namespace KafkaProducer.Services.SchemaManagement;

/// <summary>
/// Registers Kafka schemas with the Schema Registry on application startup.
/// Uses REST API directly to avoid the C# client adding metadata wrappers.
/// TODO: still unsure if this is the best approach - surely there's some form of codegen solution to make this easier 
/// </summary>
public class SchemaInitializer : IHostedService
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<SchemaInitializer> _logger;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _schemasPath;

    public SchemaInitializer(
        IConfiguration configuration,
        ILogger<SchemaInitializer> logger,
        IHttpClientFactory httpClientFactory,
        IWebHostEnvironment environment)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClientFactory = httpClientFactory;
        
        // Check both locations: during dotnet run (project dir) and after build (output dir)
        var projectSchemaPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Schemas");
        var outputSchemaPath = Path.Combine(environment.ContentRootPath, "Schemas");
        
        _schemasPath = Directory.Exists(projectSchemaPath) 
            ? projectSchemaPath 
            : outputSchemaPath;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        try
        {
            await RegisterAllSchemas(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to register schemas. Application cannot start without schemas.");
            throw;
        }
    }

    private async Task RegisterAllSchemas(CancellationToken cancellationToken)
    {
        if (!Directory.Exists(_schemasPath))
        {
            _logger.LogWarning("Schemas directory not found at {Path}. No schemas to register.", _schemasPath);
            return;
        }

        var schemaFiles = Directory.GetFiles(_schemasPath, "*.json");
        
        if (schemaFiles.Length == 0)
        {
            _logger.LogWarning("No schema files found in {Path}", _schemasPath);
            return;
        }

        _logger.LogInformation("Found {Count} schema file(s) to register", schemaFiles.Length);

        foreach (var schemaFile in schemaFiles)
        {
            await RegisterSchema(schemaFile, cancellationToken);
        }
        
        _logger.LogInformation("Schema registration complete. Registered {Count} schema(s)", schemaFiles.Length);
    }

    private async Task RegisterSchema(string schemaFilePath, CancellationToken cancellationToken)
    {
        var fileName = Path.GetFileNameWithoutExtension(schemaFilePath);
        
        // Convention: "orders.json" → topic "orders", subject "orders-value"
        var subjectName = $"{fileName}-value";
        
        _logger.LogInformation("Registering schema: {FileName}.json → Subject: {Subject}", 
            fileName, subjectName);

        var schemaJson = await File.ReadAllTextAsync(schemaFilePath, cancellationToken);
        
        // Use REST API directly to avoid C# client adding metadata
        var kafkaConfig = _configuration.GetSection(KafkaConfiguration.SectionName).Get<KafkaConfiguration>();
        var schemaRegistryUrl = kafkaConfig?.SchemaRegistryUrl ?? throw new InvalidOperationException("Schema Registry URL is not configured");
        
        var requestBody = new
        {
            schema = schemaJson,
            schemaType = "JSON"
        };
        
        var httpClient = _httpClientFactory.CreateClient();
        var content = new StringContent(
            JsonSerializer.Serialize(requestBody), 
            Encoding.UTF8, 
            "application/vnd.schemaregistry.v1+json"
        );
        
        var response = await httpClient.PostAsync(
            $"{schemaRegistryUrl}/subjects/{subjectName}/versions", 
            content, 
            cancellationToken
        );
        
        if (response.IsSuccessStatusCode)
        {
            var result = await response.Content.ReadAsStringAsync(cancellationToken);
            var schemaResponse = JsonSerializer.Deserialize<SchemaRegistrationResponse>(result);
            _logger.LogInformation("✓ Schema '{FileName}' registered with ID: {SchemaId}", fileName, schemaResponse?.Id);
        }
        else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            _logger.LogInformation("✓ Schema '{FileName}' already registered (subject: {Subject})", fileName, subjectName);
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Failed to register schema '{fileName}': {response.StatusCode} - {errorContent}");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
    
    private class SchemaRegistrationResponse
    {
        public int Id { get; set; }
    }
}
