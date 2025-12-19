using Common;
using OrderConsumerService = OrderConsumer.Services.OrderConsumer;

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<KafkaConfiguration>(
    builder.Configuration.GetRequiredSection(KafkaConfiguration.SectionName));

builder.Services.AddHostedService<OrderConsumerService>();

var app = builder.Build();

await app.RunAsync();