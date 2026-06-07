using Microsoft.AspNetCore.Authentication.JwtBearer;
using OrderSphere.Advisory.Api.Agent;

var builder = WebApplication.CreateBuilder(args);

// Aspire defaults (OpenTelemetry, health checks, service discovery, resilience).
builder.AddServiceDefaults();

builder.Services.AddHttpContextAccessor();

// Keycloak JWT validation. The end-user token is forwarded by the BFF; the agent
// passes it on to the MCP server. Audience is validated downstream, not here.
var authority = builder.Configuration["Keycloak:Authority"];
var authEnabled = !string.IsNullOrWhiteSpace(authority);
if (authEnabled)
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            options.Authority = authority;
            options.RequireHttpsMetadata = false;
            options.TokenValidationParameters.ValidateAudience = false;
        });
    builder.Services.AddAuthorization();
}

builder.Services.AddSingleton<AdvisorChatService>();

var app = builder.Build();

if (authEnabled)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.MapDefaultEndpoints();
app.MapAdvisorEndpoints();

app.Run();
