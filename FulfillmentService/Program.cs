using Common;
using FulfillmentServiceWorker = FulfillmentService.Services.FulfillmentService;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<KafkaConfiguration>(
    builder.Configuration.GetRequiredSection(KafkaConfiguration.SectionName));

builder.Services.AddHostedService<FulfillmentServiceWorker>();

var app = builder.Build();

await app.RunAsync();