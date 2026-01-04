using Microsoft.AspNetCore.Mvc;
using Common;
using Confluent.SchemaRegistry;
using OrderService.Services;
using OrderService.Data;
using OrderService.Repositories;
using OrderService.Models;
using OrderService.Models.DTOs;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

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

builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();

builder.Services.AddSingleton<IOrderProducer, OrderProducer>();
builder.Services.AddScoped<IOrderService, OrderService.Services.OrderService>();

var app = builder.Build();

var connectionString = builder.Configuration.GetConnectionString("OrdersDatabase")
    ?? throw new InvalidOperationException("OrdersDatabase connection string is not configured");

DatabaseMigrator.MigrateDatabase(connectionString, app.Logger);

app.MapPost("/orders", async ([FromBody] CreateOrderRequest order, IOrderService service) => await service.CreateOrder(new Order(order)));

await app.RunAsync();