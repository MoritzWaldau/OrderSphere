using OrderSphere.Notification.Worker.Email;
using OrderSphere.Notification.Worker.Workers;

var builder = Host.CreateApplicationBuilder(args);

builder.AddServiceDefaults();

// Azure Service Bus
builder.AddAzureServiceBusClient("azure-service-bus");

// Email service — reads connection string and sender address from configuration
var mailConfig = builder.Configuration.GetSection("MailServiceConfiguration");
var connectionString = mailConfig["ConnectionString"];
var senderAddress = mailConfig["SenderAddress"];

builder.Services.AddSingleton(sp =>
    new NotificationEmailService(
        connectionString,
        senderAddress,
        sp.GetRequiredService<ILogger<NotificationEmailService>>()));

builder.Services.AddHostedService<NotificationProcessor>();

builder.Build().Run();
