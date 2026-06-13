using Microsoft.EntityFrameworkCore;
using OrderSphere.UserProfile.Api.Configuration;
using OrderSphere.UserProfile.Api.Endpoints;
using OrderSphere.UserProfile.Application;
using OrderSphere.UserProfile.Infrastructure;
using OrderSphere.UserProfile.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.AddOrderSphereSwagger("OrderSphere UserProfile API");

// Domain layers
builder.AddUserProfileInfrastructure();        // EF Core, IUserProfileDbContext
builder.Services.AddUserProfileApplication();  // MediatR + Behaviors + FluentValidation

// JWT Bearer — shared Keycloak validation; audience "userprofile-api" is a
// dedicated bearer-only client in the Keycloak realm.
builder.AddOrderSphereJwtAuth("userprofile-api");
builder.Services.AddCurrentUser();

builder.AddOrderSphereExceptionHandling();
builder.Services.AddUserProfileApiVersioning();

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("AdminPolicy", policy => policy.RequireRole("admin"));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<UserProfileDbContext>().Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseOrderSphereSwagger(docTitle: "OrderSphere UserProfile API");
}

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();

app.MapUserProfileEndpoints();

app.MapDefaultEndpoints();

app.Run();
