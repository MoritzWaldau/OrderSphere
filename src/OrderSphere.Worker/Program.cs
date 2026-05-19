using OrderSphere.Hosting;
using OrderSphere.Infrastructure;
using OrderSphere.Worker.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddOrderSphereCore();

builder.Services.AddHostedService<OrderProcessor>();
builder.Services.AddOutboxProcessing();

builder.Build().Run();
