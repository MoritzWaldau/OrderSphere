var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// The gateway validates that the token is a legitimate Keycloak token before
// forwarding it downstream. Downstream services each re-validate the token
// against their own dedicated audience ("ordering-api", "catalog-api", etc.).
// The gateway audience "account" matches the realm's default access-token audience
// until the per-service bearer clients are provisioned in the realm.
builder.AddOrderSphereJwtAuth(
    builder.Configuration["Keycloak:Audience"] ?? "account");

builder.Services.AddAuthorization();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

app.MapDefaultEndpoints();

app.UseAuthentication();
app.UseAuthorization();

app.MapReverseProxy();

app.Run();
