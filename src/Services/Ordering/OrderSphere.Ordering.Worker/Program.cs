using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.Behaviors;
using OrderSphere.BuildingBlocks.EventBus.AzureServiceBus;
using OrderSphere.Ordering.Infrastructure;
using OrderSphere.Ordering.Infrastructure.Persistence;
using OrderSphere.Ordering.Worker.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Azure Service Bus
builder.AddAzureServiceBusClient("azure-service-bus");

// EventBus abstraction
builder.Services.AddAzureServiceBusEventBus();

// EF Core — Aspire injects connection string via "ordering-db"
builder.AddNpgsqlDbContext<OrderingDbContext>("ordering-db", settings =>
{
    settings.DisableRetry = false;
});

// Ordering infrastructure (outbox handler registrations, DI bindings)
builder.Services.AddOrderingInfrastructure(builder.Environment);
builder.Services.AddOrderingOutboxProcessing();

// Service Bus consumers
builder.Services.AddHostedService<OrderProcessor>();
builder.Services.AddHostedService<PaymentResultProcessor>();

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddTransient(typeof(INotificationHandler<>), typeof(DomainEventLoggingHandler<>));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<OrderingDbContext>().Database.Migrate();
}

// Liveness/readiness endpoints (/health, /alive, /version) for container probes.
app.MapDefaultEndpoints();

app.Run();
