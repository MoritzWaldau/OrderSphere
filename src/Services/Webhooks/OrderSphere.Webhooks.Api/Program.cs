using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using OrderSphere.Webhooks.Api.Configuration;
using OrderSphere.Webhooks.Api.Endpoints;
using OrderSphere.Webhooks.Application;
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

builder.AddOrderSphereExceptionHandling();
builder.Services.AddWebhooksApiVersioning();

builder.AddOrderSphereJwtAuth("webhooks-api");

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<WebhooksDbContext>().Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
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
