using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.EventBus.AzureServiceBus.Inbox;
using OrderSphere.BuildingBlocks.EventBus.Inbox;
using OrderSphere.Notification.Worker.Email;
using OrderSphere.Notification.Worker.Persistence;
using OrderSphere.Notification.Worker.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// PostgreSQL — inbox for idempotent message processing
builder.AddNpgsqlDbContext<NotificationDbContext>("notification-db");
builder.Services.AddScoped<IInboxStore, EfInboxStore<NotificationDbContext>>();

// Azure Service Bus
builder.AddAzureServiceBusClient("azure-service-bus");

// Email service — reads connection string and sender address from configuration
var mailConfig = builder.Configuration.GetSection("MailServiceConfiguration");
var connectionString = mailConfig["ConnectionString"]
    ?? throw new InvalidOperationException("MailServiceConfiguration:ConnectionString is not configured.");
var senderAddress = mailConfig["SenderAddress"]
    ?? throw new InvalidOperationException("MailServiceConfiguration:SenderAddress is not configured.");

builder.Services.AddSingleton(sp =>
    new NotificationEmailService(
        connectionString,
        senderAddress,
        sp.GetRequiredService<ILogger<NotificationEmailService>>()));

builder.Services.AddHostedService<NotificationProcessor>();

var host = builder.Build();

// Apply EF migrations on startup (dev convenience)
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    db.Database.Migrate();
}

host.Run();
