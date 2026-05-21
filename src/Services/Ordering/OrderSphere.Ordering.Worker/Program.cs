using Microsoft.EntityFrameworkCore;
using OrderSphere.Ordering.Infrastructure;
using OrderSphere.Ordering.Infrastructure.Email;
using OrderSphere.Ordering.Infrastructure.Persistence;
using OrderSphere.Ordering.Worker.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// Azure Service Bus
builder.AddAzureServiceBusClient("azure-service-bus");

// EF Core — Aspire injects connection string via "ordering-db"
builder.AddNpgsqlDbContext<OrderingDbContext>("ordering-db", settings =>
{
    settings.DisableRetry = false;
});

// Ordering infrastructure (email, DI bindings)
builder.Services.AddOrderingInfrastructure(builder.Environment);
builder.Services.Configure<OrderingMailConfiguration>(
    builder.Configuration.GetSection("MailServiceConfiguration"));

// Service Bus consumer
builder.Services.AddHostedService<OrderProcessor>();

builder.Build().Run();
