using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.Behaviors;
using OrderSphere.BuildingBlocks.EventBus.AzureServiceBus;
using OrderSphere.Ordering.Application.Abstractions;
using OrderSphere.Ordering.Api.Authorization;
using OrderSphere.Ordering.Infrastructure.CatalogClient;
using HttpBasketClient = OrderSphere.Ordering.Infrastructure.CatalogClient.HttpBasketClient;
using OrderSphere.Ordering.Api.Configuration;
using OrderSphere.Ordering.Api.Endpoints;
using OrderSphere.Ordering.Api.Exceptions;
using OrderSphere.Ordering.Infrastructure;
using OrderSphere.Ordering.Infrastructure.Idempotency;
using OrderSphere.Ordering.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddOrderSphereSwagger("OrderSphere Ordering API");

// EF Core — Aspire injects connection string via "ordering-db"
builder.AddNpgsqlDbContext<OrderingDbContext>("ordering-db", settings =>
{
    settings.DisableRetry = false;
});

// Ordering Infrastructure
builder.Services.AddOrderingInfrastructure(builder.Environment);
builder.Services.AddOrderingOutboxProcessing();
// Azure Service Bus (for OutboxDispatcher → RealServiceBusPublisher)
builder.AddAzureServiceBusClient("azure-service-bus");

// EventBus abstraction
builder.Services.AddAzureServiceBusEventBus();

// Distributed cache (Redis) for checkout idempotency key deduplication (30-min TTL per key).
// Redis-backed so the guard holds across multiple Ordering.Api instances.
builder.AddRedisDistributedCache("redis");
builder.Services.AddScoped<ICheckoutIdempotencyStore, RedisCheckoutIdempotencyStore>();

// MediatR — scan this assembly for handlers + pipeline behaviors
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(IOrderingDbContext).Assembly);
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});
builder.Services.AddTransient(typeof(INotificationHandler<>), typeof(DomainEventLoggingHandler<>));

// FluentValidation — scan this assembly for validators
builder.Services.AddValidatorsFromAssembly(typeof(IOrderingDbContext).Assembly);

// HTTP client for Catalog service (internal stock operations).
// ClientCredentialsTokenHandler acquires a Keycloak client_credentials token (configured
// via Keycloak:ClientId / Keycloak:ClientSecret) and forwards it as a Bearer token.
// Note: AddStandardResilienceHandler is applied globally via AddServiceDefaults().
builder.Services.AddHttpClient<ICatalogClient, HttpCatalogClient>(client =>
{
    var catalogUrl = builder.Configuration["Services:Catalog:BaseUrl"]
        ?? "http://ordersphere-catalog";
    client.BaseAddress = new Uri(catalogUrl);
}).AddClientCredentialsHandler();

// HTTP client for Basket service (cart read/clear during checkout).
builder.Services.AddHttpClient<IBasketClient, HttpBasketClient>(client =>
{
    var basketUrl = builder.Configuration["Services:Basket:BaseUrl"]
        ?? "http://ordersphere-basket";
    client.BaseAddress = new Uri(basketUrl);
}).AddClientCredentialsHandler();

// Health checks
var orderingConnectionString = builder.Configuration.GetConnectionString("ordering-db") ?? "";
builder.Services.AddHealthChecks()
    .AddNpgSql(orderingConnectionString, name: "postgres");

// Validation exception → HTTP 400
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddProblemDetails();

// JWT Bearer — shared Keycloak validation; audience "ordering-api" is a
// dedicated bearer-only client in the Keycloak realm.
builder.AddOrderSphereJwtAuth("ordering-api");
builder.Services.AddCurrentUser();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AuthorizationPolicies.Admin,
        p => p.RequireRole("admin"))
    .AddPolicy(AuthorizationPolicies.Staff,
        p => p.RequireRole("csr", "order-manager", "admin"))
    .AddPolicy(AuthorizationPolicies.OrderManager,
        p => p.RequireRole("order-manager", "admin"))
    .AddPolicy(AuthorizationPolicies.OrderOwnerOrStaff,
        p => p.AddRequirements(new OrderOwnerOrStaffRequirement()));

// ABAC handler — registered as singleton (stateless, no scoped dependencies).
builder.Services.AddSingleton<IAuthorizationHandler, OrderOwnerOrStaffHandler>();

var app = builder.Build();

// Apply EF migrations on startup (dev convenience)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
    db.Database.Migrate();

    app.UseOrderSphereSwagger(docTitle: "OrderSphere Ordering API");
}

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapCheckoutEndpoints();
app.MapCouponEndpoints();
app.MapOrderEndpoints();

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapDefaultEndpoints();

app.Run();
