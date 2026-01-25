using Common;
using FulfillmentService.Configuration;
using FulfillmentService.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

var loggerConfiguration = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.WithProperty("Service", "FulfillmentService")
    .WriteTo.Console();

if (builder.Environment.IsDevelopment())
{
    loggerConfiguration.WriteTo.Seq(builder.Configuration["Seq:ServerUrl"] ?? "http://localhost:5341");
}

Log.Logger = loggerConfiguration.CreateLogger();

builder.Host.UseSerilog();

builder.Services.Configure<KafkaConfiguration>(
    builder.Configuration.GetRequiredSection(KafkaConfiguration.SectionName));

builder.Services.Configure<WorkerPoolOptions>(
    builder.Configuration.GetRequiredSection(WorkerPoolOptions.SectionName));

builder.Services.AddSingleton<IFulfillmentService, FulfillmentService.Services.FulfillmentService>();
builder.Services.AddSingleton<IOrderWorkerPool, OrderWorkerPool>();
builder.Services.AddHostedService(sp => (OrderWorkerPool)sp.GetRequiredService<IOrderWorkerPool>());
builder.Services.AddHostedService<OrderConsumer>();

var app = builder.Build();

app.UseSerilogRequestLogging();

await app.RunAsync();