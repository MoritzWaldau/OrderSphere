using Microsoft.AspNetCore.Builder;
using OrderSphere.Webhooks.Application;
using OrderSphere.Webhooks.Infrastructure;
using OrderSphere.Webhooks.Worker.Workers;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Redis distributed lock — guards WebhookDeliveryProcessor against double-delivery at scale.
await builder.AddOrderSphereRedisAsync();
builder.Services.AddOrderSphereDistributedLocking();

builder.AddWebhooksInfrastructure();
builder.Services.AddWebhooksApplication();

builder.AddAzureServiceBusClient("azure-service-bus");

builder.Services.AddHttpClient("WebhookDelivery", client =>
{
    client.Timeout = TimeSpan.FromSeconds(10);
    client.DefaultRequestHeaders.Add("User-Agent", "OrderSphere-Webhooks/1.0");
});

builder.Services.AddHostedService<WebhookEventProcessor>();
builder.Services.AddHostedService<WebhookDeliveryProcessor>();

var app = builder.Build();

// Liveness/readiness endpoints (/health, /alive, /version) for container probes.
app.MapDefaultEndpoints();

app.Run();
