using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.Behaviors;
using OrderSphere.UserProfile.Api.Endpoints;
using OrderSphere.UserProfile.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// EF Core — Aspire injects the connection string via "userprofile-db"
builder.AddNpgsqlDbContext<UserProfileDbContext>("userprofile-db", settings =>
{
    settings.DisableRetry = false;
});

// JWT Bearer — shared Keycloak validation; audience "userprofile-api" is a
// dedicated bearer-only client in the Keycloak realm.
builder.AddOrderSphereJwtAuth("userprofile-api");
builder.Services.AddCurrentUser();

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly);
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});
builder.Services.AddTransient(typeof(INotificationHandler<>), typeof(DomainEventLoggingHandler<>));

builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminPolicy", policy => policy.RequireRole("admin"));

var app = builder.Build();

// Apply EF migrations on startup (dev convenience)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<UserProfileDbContext>();
    db.Database.Migrate();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapProfileEndpoints();
app.MapAdminProfileEndpoints();

app.MapDefaultEndpoints();

app.Run();
