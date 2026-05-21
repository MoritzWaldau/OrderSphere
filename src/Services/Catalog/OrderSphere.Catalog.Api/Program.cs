using FluentValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OrderSphere.Catalog.Api.Endpoints;
using OrderSphere.Catalog.Api.Exceptions;
using OrderSphere.Catalog.Api.Grpc;
using OrderSphere.Catalog.Infrastructure.Persistence;
using OrderSphere.Domain.Behaviors;

var builder = WebApplication.CreateBuilder(args);

// EF Core — Aspire injects the connection string via "catalog-db"
builder.AddNpgsqlDbContext<CatalogDbContext>("catalog-db", settings =>
{
    settings.DisableRetry = false;
});

// HybridCache (in-memory + optional Redis)
builder.Services.AddHybridCache();
builder.AddRedisDistributedCache("redis");

// MediatR — scan this assembly for handlers + pipeline behaviors
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});

// FluentValidation — scan this assembly for validators
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// Health checks
var catalogConnectionString = builder.Configuration.GetConnectionString("catalog-db") ?? "";
builder.Services.AddHealthChecks()
    .AddNpgSql(catalogConnectionString, name: "postgres");

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

// gRPC
builder.Services.AddGrpc();

var app = builder.Build();

// Apply EF migrations on startup (dev convenience)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    db.Database.Migrate();
}

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<CatalogGrpcService>();

app.MapProductEndpoints();
app.MapCategoryEndpoints();
app.MapInternalProductEndpoints();

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });

app.Run();
