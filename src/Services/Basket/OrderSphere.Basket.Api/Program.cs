using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using OrderSphere.Basket.Api.Configuration;
using OrderSphere.Basket.Api.Endpoints;
using OrderSphere.Basket.Application;
using OrderSphere.Basket.Infrastructure;
using OrderSphere.Basket.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddOrderSphereSwagger("OrderSphere Basket API");

builder.AddBasketInfrastructure();
builder.Services.AddBasketApplication();

// gRPC client for Catalog service (internal stock checks). Internal Catalog endpoints are
// network-protected (no auth), so no client-credentials handler is attached.
builder.AddCatalogGrpcClient();

// Health checks
var basketConnectionString = builder.Configuration.GetConnectionString("basket-db") ?? "";
builder.Services.AddHealthChecks()
    .AddNpgSql(basketConnectionString, name: "postgres");

builder.Services.AddBasketApiVersioning();
builder.Services.AddBasketRateLimiting();

// Validation exception → HTTP 400
builder.AddOrderSphereExceptionHandling();

// JWT Bearer
builder.AddOrderSphereJwtAuth("basket-api");
builder.Services.AddCurrentUser();

var app = builder.Build();

// Skipped under "Testing": integration tests boot with a non-relational in-memory provider
// where the relational Migrate() would throw. See OrderSphere.IntegrationTests.Api.
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<BasketDbContext>().Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseOrderSphereSwagger(docTitle: "OrderSphere Basket API");
}

app.UseExceptionHandler();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.UseOrderSphereRequestLogging();

app.MapCartEndpoints();
app.MapInternalCartEndpoints();

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapDefaultEndpoints();

app.Run();
