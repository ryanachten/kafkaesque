using Common;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

// Build configuration
var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

// Build service provider for logging
var serviceProvider = new ServiceCollection()
    .AddLogging(builder =>
    {
        builder.AddConfiguration(configuration.GetSection("Logging"));
        builder.AddConsole();
    })
    .AddHttpClient()
    .AddSingleton<IConfiguration>(configuration)
    .BuildServiceProvider();

var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();

try
{
    logger.LogInformation("=== Schema Manager Started ===");
    await RegisterAllSchemas(configuration, logger, httpClientFactory);
    logger.LogInformation("=== Schema Manager Completed Successfully ===");
    Environment.Exit(0);
}
catch (Exception ex)
{
    logger.LogError(ex, "Failed to register schemas");
    Environment.Exit(1);
}

static async Task RegisterAllSchemas(IConfiguration configuration, ILogger logger, IHttpClientFactory httpClientFactory)
{
    var schemasPath = Path.Combine(Directory.GetCurrentDirectory(), "Schemas");

    if (!Directory.Exists(schemasPath))
    {
        logger.LogWarning("Schemas directory not found at {Path}. No schemas to register.", schemasPath);
        return;
    }

    var schemaFiles = Directory.GetFiles(schemasPath, "*.json");

    if (schemaFiles.Length == 0)
    {
        logger.LogWarning("No schema files found in {Path}", schemasPath);
        return;
    }

    logger.LogInformation("Found {Count} schema file(s) to register", schemaFiles.Length);

    foreach (var schemaFile in schemaFiles)
    {
        await RegisterSchema(schemaFile, configuration, logger, httpClientFactory);
    }

    logger.LogInformation("Schema registration complete. Registered {Count} schema(s)", schemaFiles.Length);
}

static async Task RegisterSchema(string schemaFilePath, IConfiguration configuration, ILogger logger, IHttpClientFactory httpClientFactory)
{
    var fileName = Path.GetFileNameWithoutExtension(schemaFilePath);

    // Convention: "orders.json" → topic "orders", subject "orders-value"
    var subjectName = $"{fileName}-value";

    logger.LogInformation("Registering schema: {FileName}.json → Subject: {Subject}",
        fileName, subjectName);

    var schemaJson = await File.ReadAllTextAsync(schemaFilePath);

    // Use REST API directly to avoid C# client adding metadata
    var kafkaConfig = configuration.GetSection(KafkaConfiguration.SectionName).Get<KafkaConfiguration>();
    var schemaRegistryUrl = kafkaConfig?.SchemaRegistryUrl ?? throw new InvalidOperationException("Schema Registry URL is not configured");

    var requestBody = new
    {
        schema = schemaJson,
        schemaType = "JSON"
    };

    var httpClient = httpClientFactory.CreateClient();
    var content = new StringContent(
        JsonSerializer.Serialize(requestBody),
        Encoding.UTF8,
        "application/vnd.schemaregistry.v1+json"
    );

    var response = await httpClient.PostAsync(
        $"{schemaRegistryUrl}/subjects/{subjectName}/versions",
        content
    );

    if (response.IsSuccessStatusCode)
    {
        var result = await response.Content.ReadAsStringAsync();
        var schemaResponse = JsonSerializer.Deserialize<SchemaRegistrationResponse>(result);
        logger.LogInformation("✓ Schema '{FileName}' registered with ID: {SchemaId}", fileName, schemaResponse?.Id);
    }
    else if (response.StatusCode == System.Net.HttpStatusCode.Conflict)
    {
        logger.LogInformation("✓ Schema '{FileName}' already registered (subject: {Subject})", fileName, subjectName);
    }
    else
    {
        var errorContent = await response.Content.ReadAsStringAsync();
        throw new InvalidOperationException($"Failed to register schema '{fileName}': {response.StatusCode} - {errorContent}");
    }
}

record SchemaRegistrationResponse(int Id);
