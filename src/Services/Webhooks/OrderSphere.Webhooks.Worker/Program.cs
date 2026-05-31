using MediatR;
using OrderSphere.BuildingBlocks.Behaviors;
using OrderSphere.Webhooks.Infrastructure;
using OrderSphere.Webhooks.Infrastructure.Persistence;
using OrderSphere.Webhooks.Worker.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

builder.AddNpgsqlDbContext<WebhooksDbContext>("webhooks-db");

builder.Services.AddWebhooksInfrastructure();

builder.AddAzureServiceBusClient("azure-service-bus");

builder.Services.AddHttpClient("WebhookDelivery", client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Add("User-Agent", "OrderSphere-Webhooks/1.0");
});

builder.Services.AddHostedService<WebhookEventProcessor>();
builder.Services.AddHostedService<WebhookDeliveryProcessor>();

builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
builder.Services.AddTransient(typeof(INotificationHandler<>), typeof(DomainEventLoggingHandler<>));

var host = builder.Build();
host.Run();
