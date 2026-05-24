using FluentValidation;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using OrderSphere.Basket.Api.CatalogClient;
using OrderSphere.Basket.Api.Endpoints;
using OrderSphere.Basket.Api.Exceptions;
using OrderSphere.Basket.Infrastructure.Persistence;
using OrderSphere.BuildingBlocks.Behaviors;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// EF Core — Aspire injects connection string via "basket-db"
builder.AddNpgsqlDbContext<BasketDbContext>("basket-db", settings =>
{
    settings.DisableRetry = false;
});

// MediatR — scan this assembly for handlers + pipeline behaviors
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

// FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// HTTP client for Catalog service (stock verification)
builder.Services.AddHttpClient<ICatalogClient, HttpCatalogClient>(client =>
{
    var catalogUrl = builder.Configuration["Services:Catalog:BaseUrl"]
        ?? "http://ordersphere-catalog";
    client.BaseAddress = new Uri(catalogUrl);
}).AddClientCredentialsHandler();

// Health checks
var basketConnectionString = builder.Configuration.GetConnectionString("basket-db") ?? "";
builder.Services.AddHealthChecks()
    .AddNpgSql(basketConnectionString, name: "postgres");

// Validation exception → HTTP 400
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddProblemDetails();

// JWT Bearer
builder.AddOrderSphereJwtAuth("basket-api");
builder.Services.AddCurrentUser();

var app = builder.Build();

// Apply EF migrations on startup (dev convenience)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<BasketDbContext>();
    db.Database.Migrate();
}

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapCartEndpoints();
app.MapInternalCartEndpoints();

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapDefaultEndpoints();

app.Run();
