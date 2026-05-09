using OrderSphere.Hosting;
using OrderSphere.Worker.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();
builder.AddOrderSphereCore();

builder.Services.AddHostedService<OrderProcessor>();

builder.Build().Run();
