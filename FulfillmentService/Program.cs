using Common;
using FulfillmentService.Configuration;
using FulfillmentService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<KafkaConfiguration>(
    builder.Configuration.GetRequiredSection(KafkaConfiguration.SectionName));

builder.Services.Configure<WorkerPoolOptions>(
    builder.Configuration.GetRequiredSection(WorkerPoolOptions.SectionName));

builder.Services.AddSingleton<IFulfillmentService, FulfillmentService.Services.FulfillmentService>();
builder.Services.AddSingleton<IOrderWorkerPool, OrderWorkerPool>();
builder.Services.AddHostedService(sp => (OrderWorkerPool)sp.GetRequiredService<IOrderWorkerPool>());
builder.Services.AddHostedService<OrderConsumer>();

var app = builder.Build();

await app.RunAsync();