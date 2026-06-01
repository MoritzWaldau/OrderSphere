using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using OrderSphere.Payment.Application;
using OrderSphere.Payment.Api.Endpoints;
using OrderSphere.Payment.Api.Exceptions;
using OrderSphere.Payment.Infrastructure;
using OrderSphere.Payment.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddOrderSphereSwagger("OrderSphere Payment API");

builder.AddNpgsqlDbContext<PaymentDbContext>("payment-db", settings =>
{
    settings.DisableRetry = false;
});

builder.Services.AddPaymentInfrastructure(builder.Configuration);

builder.Services.AddPaymentApplication();

var paymentConnectionString = builder.Configuration.GetConnectionString("payment-db") ?? "";
builder.Services.AddHealthChecks()
    .AddNpgSql(paymentConnectionString, name: "postgres");

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();

builder.AddOrderSphereJwtAuth("payment-api");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    db.Database.Migrate();

    app.UseOrderSphereSwagger(docTitle: "OrderSphere Payment API");
}

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapPaymentEndpoints();
app.MapInternalPaymentEndpoints();

app.MapHealthChecks("/health");
app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
app.MapDefaultEndpoints();

app.Run();
