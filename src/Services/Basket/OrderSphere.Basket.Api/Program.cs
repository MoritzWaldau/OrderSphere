using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using OrderSphere.Basket.Application;
using OrderSphere.Basket.Application.Abstractions;
using OrderSphere.Basket.Api.Endpoints;
using OrderSphere.Basket.Api.Exceptions;
using OrderSphere.Basket.Infrastructure;
using OrderSphere.Basket.Infrastructure.CatalogClient;
using OrderSphere.Basket.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddOrderSphereSwagger("OrderSphere Basket API");

builder.AddBasketInfrastructure();
builder.Services.AddBasketApplication();

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

    app.UseOrderSphereSwagger(docTitle: "OrderSphere Basket API");
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
