using OrderSphere.BuildingBlocks.EventBus.AzureServiceBus;
using OrderSphere.Payment.Infrastructure;
using OrderSphere.Payment.Infrastructure.Persistence;
using OrderSphere.Payment.Worker.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<PaymentDbContext>("payment-db");
builder.AddAzureServiceBusClient("azure-service-bus");

builder.Services.AddAzureServiceBusEventBus();
builder.Services.AddPaymentInfrastructure();
builder.Services.AddHostedService<PaymentProcessor>();

var host = builder.Build();
host.Run();
