using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchemaManager.Options;
using SchemaManager.Services.SchemaGeneration;
using SchemaManager.Services.SchemaRegistration;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "Production"}.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

var serviceProvider = new ServiceCollection()
    .AddLogging(builder =>
    {
        builder.AddConfiguration(configuration.GetSection("Logging"));
        builder.AddConsole();
    })
    .AddHttpClient()
    .AddSingleton<IConfiguration>(configuration)
    .Configure<SchemaRegistrationOptions>(configuration.GetSection(SchemaRegistrationOptions.SectionName))
    .AddSingleton<ISchemaGenerationService, SchemaGenerationService>()
    .AddSingleton<ISchemaRegistrationService, SchemaRegistrationService>()
    .BuildServiceProvider();

var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
var schemaGenerationService = serviceProvider.GetRequiredService<ISchemaGenerationService>();
var schemaRegistrationService = serviceProvider.GetRequiredService<ISchemaRegistrationService>();

try
{
    // Step 1: Generate C# code from Avro schemas
    logger.LogInformation("Starting code generation from Avro schemas...");
    await schemaGenerationService.GenerateCodeFromSchemas();

    // Step 2: Register schemas to Schema Registry
    logger.LogInformation("Waiting for Schema Registry...");
    await schemaRegistrationService.WaitForSchemaRegistry();

    logger.LogInformation("Registering schemas...");
    await schemaRegistrationService.RegisterSchemas();
    
    logger.LogInformation("Schema management complete");
    Environment.Exit(0);
}
catch (Exception ex)
{
    logger.LogError(ex, "Failed to complete schema management");
    Environment.Exit(1);
}
