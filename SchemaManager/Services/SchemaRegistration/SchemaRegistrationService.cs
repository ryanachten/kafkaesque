using Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SchemaManager.Options;
using System.Text;
using System.Text.Json;

namespace SchemaManager.Services.SchemaRegistration;

public class SchemaRegistrationService(
    IConfiguration configuration,
    IOptions<SchemaRegistrationOptions> options,
    ILogger<SchemaRegistrationService> logger,
    IHttpClientFactory httpClientFactory) : ISchemaRegistrationService
{
    private const string SchemasPath = "Schemas";
    private readonly IConfiguration _configuration = configuration;
    private readonly SchemaRegistrationOptions _options = options.Value;
    private readonly ILogger<SchemaRegistrationService> _logger = logger;
    private readonly IHttpClientFactory _httpClientFactory = httpClientFactory;

    public async Task WaitForSchemaRegistry(CancellationToken cancellationToken = default)
    {
        var kafkaConfig = _configuration.GetSection(KafkaConfiguration.SectionName).Get<KafkaConfiguration>();
        var schemaRegistryUrl = kafkaConfig?.SchemaRegistryUrl ?? throw new InvalidOperationException("Schema Registry URL is not configured");

        var httpClient = _httpClientFactory.CreateClient();
        var maxRetries = _options.MaxRetries;
        var retryDelay = TimeSpan.FromSeconds(_options.RetryDelaySeconds);

        _logger.LogInformation("Waiting for Schema Registry at {Url} to be ready...", schemaRegistryUrl);

        for (int i = 1; i <= maxRetries; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                var response = await httpClient.GetAsync($"{schemaRegistryUrl}/subjects", cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation("Schema Registry is ready");
                    return;
                }
            }
            catch (HttpRequestException)
            {
                // Expected while service is starting up
            }

            _logger.LogInformation("Schema Registry not ready yet (attempt {Attempt}/{MaxRetries}), waiting {Delay} seconds...",
                i, maxRetries, retryDelay.TotalSeconds);
            await Task.Delay(retryDelay, cancellationToken);
        }

        throw new InvalidOperationException($"Schema Registry at {schemaRegistryUrl} did not become ready after {maxRetries} attempts");
    }

    public async Task RegisterSchemas(CancellationToken cancellationToken = default)
    {
        var schemasPath = Path.Combine(Directory.GetCurrentDirectory(), SchemasPath);

        if (!Directory.Exists(schemasPath))
        {
            _logger.LogWarning("Schemas directory not found at {Path}. No schemas to register.", schemasPath);
            return;
        }

        var schemaFiles = Directory.GetFiles(schemasPath, "*.avsc");

        if (schemaFiles.Length == 0)
        {
            _logger.LogWarning("No schema files found in {Path}", schemasPath);
            return;
        }

        _logger.LogInformation("Found {Count} schema file(s) to register", schemaFiles.Length);

        foreach (var schemaFile in schemaFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await RegisterSchemaAsync(schemaFile, cancellationToken);
        }

        _logger.LogInformation("Schema registration complete. Registered {Count} schema(s)", schemaFiles.Length);
    }

    private async Task RegisterSchemaAsync(string schemaFilePath, CancellationToken cancellationToken = default)
    {
        var fileName = Path.GetFileNameWithoutExtension(schemaFilePath);

        // Convention: "orders.avsc" → topic "orders", subject "orders-value"
        var subjectName = $"{fileName}-value";

        _logger.LogInformation("Registering schema: {FileName}.avsc → Subject: {Subject}",
            fileName, subjectName);

        var schemaContent = await File.ReadAllTextAsync(schemaFilePath, cancellationToken);

        // Use REST API directly to avoid C# client adding metadata
        var kafkaConfig = _configuration.GetSection(KafkaConfiguration.SectionName).Get<KafkaConfiguration>();
        var schemaRegistryUrl = kafkaConfig?.SchemaRegistryUrl ?? throw new InvalidOperationException("Schema Registry URL is not configured");

        var requestBody = new
        {
            schema = schemaContent,
            schemaType = "AVRO"
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
            _logger.LogInformation("Schema '{FileName}' registered with ID: {SchemaId}", fileName, schemaResponse?.Id);
        }
        else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
        {
            _logger.LogInformation("Schema '{FileName}' already registered (subject: {Subject})", fileName, subjectName);
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Failed to register schema '{fileName}': {response.StatusCode} - {errorContent}");
        }
    }

    private record SchemaRegistrationResponse(int Id);
}


