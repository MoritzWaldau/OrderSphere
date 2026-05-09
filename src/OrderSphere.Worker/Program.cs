using OrderSphere.Application;
using OrderSphere.Infrastructure;
using OrderSphere.Worker;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddAzureServiceBusClient("azure-service-bus");

builder.Services
    .AddApplicationServices(builder.Configuration)
    .AddInfrastructureServices(builder.Configuration);

builder.Services.AddHostedService<OrderProcessor>();

builder.Build().Run();