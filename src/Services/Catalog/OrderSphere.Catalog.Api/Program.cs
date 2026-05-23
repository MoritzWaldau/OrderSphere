using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using OrderSphere.Catalog.Api.Configuration;
using OrderSphere.Catalog.Api.Endpoints;
using OrderSphere.Catalog.Api.Exceptions;
using OrderSphere.Catalog.Application;
using OrderSphere.Catalog.Infrastructure;
using OrderSphere.Catalog.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Aspire defaults (OpenTelemetry, health checks, service discovery)
builder.AddServiceDefaults();

// Domain layers
builder.AddCatalogInfrastructure();          // EF Core, ICatalogDbContext, HybridCache, Redis
builder.Services.AddCatalogApplication();    // MediatR + Behaviors + FluentValidation

// Cross-cutting concerns
builder.Services.AddCatalogApiVersioning();
builder.Services.AddCatalogSwagger();
builder.Services.AddCatalogRateLimiting();
builder.AddCatalogAuthentication();     // Keycloak JWT; audience "catalog-api"
builder.Services.AddCatalogAuthorization();                          // CatalogAdminPolicy
builder.Services.AddCurrentUser();

// Exception handling
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddProblemDetails();

// Health checks (Postgres)
var catalogConnectionString = builder.Configuration.GetConnectionString("catalog-db") ?? "";
builder.Services.AddHealthChecks()
    .AddNpgSql(catalogConnectionString, name: "postgres");

// gRPC
builder.Services.AddGrpc();

var app = builder.Build();

// Dev: migrate + Swagger UI
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Database.Migrate();
    app.UseCatalogSwagger();

    var dbContext = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    dbContext.Database.EnsureCreated();
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
