using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using OrderSphere.Catalog.Api.BackgroundServices;
using OrderSphere.Catalog.Api.Configuration;
using OrderSphere.Catalog.Api.Endpoints;
using OrderSphere.Catalog.Application;
using OrderSphere.Catalog.Application.Abstractions;
using OrderSphere.Catalog.Infrastructure;
using OrderSphere.Catalog.Infrastructure.OrderingClient;
using OrderSphere.Catalog.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Aspire defaults (OpenTelemetry, health checks, service discovery)
builder.AddServiceDefaults();

// Redis distributed cache (Entra ID auth against Azure Managed Redis) — L2 for HybridCache.
await builder.AddOrderSphereRedisAsync();

// Domain layers
builder.AddCatalogInfrastructure();          // EF Core, ICatalogDbContext, HybridCache
builder.Services.AddCatalogApplication();    // MediatR + Behaviors + FluentValidation

// HTTP client for Ordering service (review purchase-verification).
// Ordering's /internal endpoints are network-protected (no auth), matching the
// internal-call pattern Catalog itself exposes; no client-credentials handler needed.
builder.Services.AddHttpClient<IOrderingClient, HttpOrderingClient>(client =>
{
    var orderingUrl = builder.Configuration["Services:Ordering:BaseUrl"]
        ?? "https://ordersphere-ordering";
    client.BaseAddress = new Uri(orderingUrl);
});

// Cross-cutting concerns
builder.Services.AddCatalogApiVersioning();
builder.Services.AddCatalogSwagger();
builder.Services.AddCatalogRateLimiting();
builder.AddCatalogAuthentication();     // Auth0 JWT; audience "catalog-api"
builder.Services.AddCatalogAuthorization();                          // CatalogAdminPolicy
builder.Services.AddCurrentUser();

// Exception handling
builder.AddOrderSphereExceptionHandling();

// Health checks (Postgres)
var catalogConnectionString = builder.Configuration.GetConnectionString("catalog-db") ?? "";
builder.Services.AddHealthChecks()
    .AddNpgSql(catalogConnectionString, name: "postgres");

// gRPC
builder.Services.AddGrpc();

// Background release of expired stock reservations (saga TTL compensation).
builder.Services.AddHostedService<ReservationSweeper>();

// Ensures the Azure AI Search index exists and seeds it when empty (no-op if unconfigured).
builder.Services.AddHostedService<CatalogSearchInitializer>();

// Ensures the product-images blob container exists (no-op if blob storage is not configured).
builder.Services.AddHostedService<BlobContainerInitializer>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseCatalogSwagger();
}

// Middleware pipeline (order matters)
app.UseExceptionHandler();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

// Endpoints
app.MapCatalogEndpoints();

// Health
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapDefaultEndpoints();

app.Run();
