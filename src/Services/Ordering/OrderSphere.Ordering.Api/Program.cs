using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OrderSphere.BuildingBlocks.Behaviors;
using OrderSphere.Ordering.Api.Abstractions;
using OrderSphere.Ordering.Api.CatalogClient;
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

// MediatR — scan this assembly for handlers + pipeline behaviors
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

// FluentValidation — scan this assembly for validators
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// HTTP client for Catalog service (internal stock operations)
// Note: AddStandardResilienceHandler is applied globally via AddServiceDefaults().
builder.Services.AddHttpClient<ICatalogClient, HttpCatalogClient>(client =>
{
    var catalogUrl = builder.Configuration["Services:Catalog:BaseUrl"]
        ?? "http://ordersphere-catalog";
    client.BaseAddress = new Uri(catalogUrl);
});

// Health checks
var orderingConnectionString = builder.Configuration.GetConnectionString("ordering-db") ?? "";
builder.Services.AddHealthChecks()
    .AddNpgSql(orderingConnectionString, name: "postgres");

// Validation exception → HTTP 400
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddProblemDetails();

// JWT Bearer (Keycloak)
var keycloakAuthority = builder.Configuration["Keycloak:Authority"]
    ?? throw new InvalidOperationException("Keycloak:Authority is required.");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = keycloakAuthority;
        options.Audience = builder.Configuration["Keycloak:Audience"] ?? "account";
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            RoleClaimType = "roles",
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminPolicy", policy => policy.RequireRole("admin"));

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

app.MapCartEndpoints();
app.MapCheckoutEndpoints();
app.MapCouponEndpoints();
app.MapOrderEndpoints();

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapDefaultEndpoints();

app.Run();
