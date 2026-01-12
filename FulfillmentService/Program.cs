using Common;
using FulfillmentService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<KafkaConfiguration>(
    builder.Configuration.GetRequiredSection(KafkaConfiguration.SectionName));

builder.Services.AddHostedService<OrderConsumer>();

var app = builder.Build();

await app.RunAsync();