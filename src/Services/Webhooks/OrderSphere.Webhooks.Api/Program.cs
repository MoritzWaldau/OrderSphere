using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using OrderSphere.Webhooks.Application;
using OrderSphere.Webhooks.Api.Endpoints;
using OrderSphere.Webhooks.Infrastructure;
using OrderSphere.Webhooks.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddOrderSphereSwagger("OrderSphere Webhooks API");

builder.AddWebhooksInfrastructure();
builder.Services.AddWebhooksApplication();

var webhooksConnectionString = builder.Configuration.GetConnectionString("webhooks-db") ?? "";
builder.Services.AddHealthChecks()
    .AddNpgSql(webhooksConnectionString, name: "postgres");

builder.Services.AddProblemDetails();

builder.AddOrderSphereJwtAuth("webhooks-api");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<WebhooksDbContext>();
    db.Database.Migrate();

    app.UseOrderSphereSwagger(docTitle: "OrderSphere Webhooks API");
}

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapWebhookEndpoints();

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapDefaultEndpoints();

app.Run();
