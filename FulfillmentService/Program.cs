using Common;
using FulfillmentService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<KafkaConfiguration>(
    builder.Configuration.GetRequiredSection(KafkaConfiguration.SectionName));
builder.Services.Configure<KafkaRetryConfiguration>(
    builder.Configuration.GetRequiredSection(KafkaRetryConfiguration.SectionName));
builder.Services.Configure<ConsumerRetryConfiguration>(
    builder.Configuration.GetRequiredSection(ConsumerRetryConfiguration.SectionName));

builder.Services.AddHostedService<OrderConsumer>();
builder.Services.AddSingleton<IOrderFulfilledProducer, OrderFulfilledProducer>();
builder.Services.AddSingleton<IOrderFulfillmentService, OrderFulfillmentService>();

var app = builder.Build();

await app.RunAsync();