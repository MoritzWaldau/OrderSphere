using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OrderSphere.Catalog.Api.Endpoints;
using OrderSphere.Catalog.Api.Grpc;
using OrderSphere.Catalog.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// EF Core — Aspire injects the connection string via "catalog-db"
builder.AddNpgsqlDbContext<CatalogDbContext>("catalog-db", settings =>
{
    settings.DisableRetry = false;
});

// HybridCache (in-memory + optional Redis)
builder.Services.AddHybridCache();
builder.AddRedisDistributedCache("redis");

// MediatR — scan this assembly for handlers
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

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

app.UseAuthentication();
app.UseAuthorization();

app.MapGrpcService<CatalogGrpcService>();

app.MapProductEndpoints();
app.MapCategoryEndpoints();
app.MapInternalProductEndpoints();

app.Run();
