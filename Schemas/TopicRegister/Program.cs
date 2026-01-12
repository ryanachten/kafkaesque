using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TopicRegister.Options;
using TopicRegister.Services;

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
    .AddSingleton<IConfiguration>(configuration)
    .Configure<TopicRegistrationOptions>(configuration.GetSection(TopicRegistrationOptions.SectionName))
    .AddSingleton<ITopicRegistrationService, TopicRegistrationService>()
    .BuildServiceProvider();

var logger = serviceProvider.GetRequiredService<ILogger<Program>>();
var topicRegistrationService = serviceProvider.GetRequiredService<ITopicRegistrationService>();

try
{
    logger.LogInformation("Waiting for Kafka broker...");
    await topicRegistrationService.WaitForKafka();

    logger.LogInformation("Registering topics...");
    await topicRegistrationService.RegisterTopics();

    logger.LogInformation("Topic registration complete");
    Environment.Exit(0);
}
catch (Exception ex)
{
    logger.LogError(ex, "Failed to complete topic registration");
    Environment.Exit(1);
}
