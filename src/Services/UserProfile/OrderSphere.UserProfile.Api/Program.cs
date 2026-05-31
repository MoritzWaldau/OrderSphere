using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.Behaviors;
using OrderSphere.UserProfile.Api.Endpoints;
using OrderSphere.UserProfile.Api.Exceptions;
using OrderSphere.UserProfile.Application.Abstractions;
using OrderSphere.UserProfile.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddOrderSphereSwagger("OrderSphere UserProfile API");

// EF Core — Aspire injects the connection string via "userprofile-db"
builder.AddNpgsqlDbContext<UserProfileDbContext>("userprofile-db", settings =>
{
    settings.DisableRetry = false;
});
builder.Services.AddScoped<IUserProfileDbContext>(sp => sp.GetRequiredService<UserProfileDbContext>());

// JWT Bearer — shared Keycloak validation; audience "userprofile-api" is a
// dedicated bearer-only client in the Keycloak realm.
builder.AddOrderSphereJwtAuth("userprofile-api");
builder.Services.AddCurrentUser();

builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(IUserProfileDbContext).Assembly);
    cfg.AddOpenBehavior(typeof(LoggingBehavior<,>));
    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
});
builder.Services.AddTransient(typeof(INotificationHandler<>), typeof(DomainEventLoggingHandler<>));

builder.Services.AddValidatorsFromAssembly(typeof(IUserProfileDbContext).Assembly);

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminPolicy", policy => policy.RequireRole("admin"));

var app = builder.Build();

// Apply EF migrations on startup (dev convenience)
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<UserProfileDbContext>();
    db.Database.Migrate();

    app.UseOrderSphereSwagger(docTitle: "OrderSphere UserProfile API");
}

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapProfileEndpoints();
app.MapAdminProfileEndpoints();

app.MapDefaultEndpoints();

app.Run();
