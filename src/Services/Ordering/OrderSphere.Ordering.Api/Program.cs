using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.Behaviors;
using OrderSphere.BuildingBlocks.EventBus.AzureServiceBus;
using OrderSphere.Ordering.Api.Abstractions;
using OrderSphere.Ordering.Api.Authorization;
using OrderSphere.Ordering.Api.CatalogClient;
using HttpBasketClient = OrderSphere.Ordering.Api.CatalogClient.HttpBasketClient;
using OrderSphere.Ordering.Api.Configuration;
using OrderSphere.Ordering.Api.Endpoints;
using OrderSphere.Ordering.Api.Exceptions;
using OrderSphere.Ordering.Infrastructure;
using OrderSphere.Ordering.Infrastructure.Email;
using OrderSphere.Ordering.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// EF Core — Aspire injects connection string via "ordering-db"
builder.AddNpgsqlDbContext<OrderingDbContext>("ordering-db", settings =>
{
    settings.DisableRetry = false;
});

// Ordering Infrastructure
builder.Services.AddOrderingInfrastructure(builder.Environment);
builder.Services.AddOrderingOutboxProcessing();
builder.Services.Configure<OrderingMailConfiguration>(
    builder.Configuration.GetSection("MailServiceConfiguration"));

// Azure Service Bus (for OutboxDispatcher → RealServiceBusPublisher)
builder.AddAzureServiceBusClient("azure-service-bus");

// EventBus abstraction
builder.Services.AddAzureServiceBusEventBus();

// MediatR — scan this assembly for handlers + pipeline behaviors
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

// FluentValidation — scan this assembly for validators
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

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
