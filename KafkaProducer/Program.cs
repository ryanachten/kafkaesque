using KafkaProducer.Models;
using KafkaProducer.Services;
using KafkaProducer.Services.SchemaManagement;
using Microsoft.AspNetCore.Mvc;
using Common;
using Confluent.SchemaRegistry;

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

builder.Services.AddHostedService<SchemaInitializer>();

builder.Services.AddSingleton<IOrderProducer, OrderProducer>();

var app = builder.Build();

app.MapPost("/orders", async ([FromBody] Order order, IOrderProducer producer) => await producer.CreateOrder(order));

app.Run();