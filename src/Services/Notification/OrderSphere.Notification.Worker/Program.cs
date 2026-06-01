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

// Email service — in development without mail config, log-only mock is used.
var mailConfig = builder.Configuration.GetSection("MailServiceConfiguration");
var connectionString = mailConfig["ConnectionString"];
var senderAddress = mailConfig["SenderAddress"];

if (!string.IsNullOrWhiteSpace(connectionString) && !string.IsNullOrWhiteSpace(senderAddress))
{
    builder.Services.AddSingleton<INotificationEmailService>(sp =>
        new NotificationEmailService(
            connectionString,
            senderAddress,
            sp.GetRequiredService<ILogger<NotificationEmailService>>()));
}
else
{
    builder.Services.AddSingleton<INotificationEmailService, LoggingNotificationEmailService>();
}

builder.Services.AddHostedService<NotificationProcessor>();

var host = builder.Build();

// Apply EF migrations on startup (dev convenience)
using (var scope = host.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<NotificationDbContext>();
    db.Database.Migrate();
}

host.Run();
