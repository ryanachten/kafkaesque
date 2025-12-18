using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SchemaGenerator.Services;

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddEnvironmentVariables()
    .Build();

var serviceProvider = new ServiceCollection()
    .AddLogging(builder =>
    {
        builder.AddConfiguration(configuration.GetSection("Logging"));
        builder.AddConsole();
    })
    .AddSingleton<IConfiguration>(configuration)
    .AddSingleton<ISchemaGenerationService, SchemaGenerationService>()
    .BuildServiceProvider();

var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
var schemaGenerationService = serviceProvider.GetRequiredService<ISchemaGenerationService>();

try
{
    logger.LogInformation("Starting code generation from Avro schemas...");
    await schemaGenerationService.GenerateCodeFromSchemas();

    logger.LogInformation("Schema code generation complete");
    Environment.Exit(0);
}
catch (Exception ex)
{
    logger.LogError(ex, "Failed to complete schema code generation");
    Environment.Exit(1);
}
