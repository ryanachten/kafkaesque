using Microsoft.AspNetCore.Mvc;
using Common;
using Confluent.SchemaRegistry;
using Schemas;
using OrderService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHttpClient();

builder.Services.Configure<KafkaConfiguration>(
    builder.Configuration.GetRequiredSection(KafkaConfiguration.SectionName));

builder.Services.AddSingleton<ISchemaRegistryClient>(sp =>
{
    var kafkaConfig = builder.Configuration
        .GetRequiredSection(KafkaConfiguration.SectionName)
        .Get<KafkaConfiguration>();

    var schemaRegistryConfig = new SchemaRegistryConfig
    {
        Url = kafkaConfig?.SchemaRegistryUrl ?? throw new InvalidOperationException("Schema Registry URL is not configured")
    };

    return new CachedSchemaRegistryClient(schemaRegistryConfig);
});

builder.Services.AddSingleton<IOrderProducer, IOrderProducer>();

var app = builder.Build();

app.MapPost("/orders", async ([FromBody] Order order, IOrderProducer producer) => await producer.CreateOrder(order));

await app.RunAsync();