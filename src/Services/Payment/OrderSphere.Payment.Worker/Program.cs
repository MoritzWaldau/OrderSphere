using OrderSphere.BuildingBlocks.EventBus.AzureServiceBus;
using OrderSphere.Payment.Application;
using OrderSphere.Payment.Infrastructure;
using OrderSphere.Payment.Infrastructure.Persistence;
using OrderSphere.Payment.Worker.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<PaymentDbContext>("payment-db");
builder.AddAzureServiceBusClient("azure-service-bus");

builder.Services.AddAzureServiceBusEventBus(); // Required by OutboxDispatcher → PaymentProcessedEventHandler
builder.Services.AddPaymentInfrastructure(builder.Configuration);
builder.Services.AddHostedService<PaymentProcessor>();

builder.Services.AddPaymentApplication();

var host = builder.Build();
host.Run();
