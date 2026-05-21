using OrderSphere.Notification.Worker.Email;
using OrderSphere.Notification.Worker.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// Azure Service Bus
builder.AddAzureServiceBusClient("azure-service-bus");

// Email service — reads connection string and sender address from configuration
var mailConfig = builder.Configuration.GetSection("MailServiceConfiguration");
var connectionString = mailConfig["ConnectionString"]
    ?? throw new InvalidOperationException("MailServiceConfiguration:ConnectionString is required.");
var senderAddress = mailConfig["SenderAddress"]
    ?? throw new InvalidOperationException("MailServiceConfiguration:SenderAddress is required.");

builder.Services.AddSingleton(sp =>
    new NotificationEmailService(
        connectionString,
        senderAddress,
        sp.GetRequiredService<ILogger<NotificationEmailService>>()));

builder.Services.AddHostedService<NotificationProcessor>();

builder.Build().Run();
