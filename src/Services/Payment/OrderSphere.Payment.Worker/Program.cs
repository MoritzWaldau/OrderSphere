using Microsoft.AspNetCore.Builder;
using OrderSphere.BuildingBlocks.EventBus.AzureServiceBus;
using OrderSphere.Payment.Application;
using OrderSphere.Payment.Infrastructure;
using OrderSphere.Payment.Infrastructure.Persistence;
using OrderSphere.Payment.Worker.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<PaymentDbContext>("payment-db");
builder.AddAzureServiceBusClient("azure-service-bus");

builder.Services.AddAzureServiceBusEventBus(); // Required by OutboxDispatcher → PaymentProcessedEventHandler
builder.Services.AddPaymentInfrastructure(builder.Configuration);
builder.Services.AddHostedService<PaymentProcessor>();
builder.Services.AddHostedService<OrderConfirmationFailedProcessor>();

builder.Services.AddPaymentApplication();

var app = builder.Build();

// Liveness/readiness endpoints (/health, /alive, /version) for container probes.
app.MapDefaultEndpoints();

app.Run();
