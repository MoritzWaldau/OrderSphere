using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using OrderSphere.Advisory.Api.Agent;
using OrderSphere.Advisory.Api.Configuration;
using OrderSphere.Advisory.Infrastructure;
using OrderSphere.Advisory.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Aspire defaults (OpenTelemetry, health checks, service discovery, resilience).
builder.AddServiceDefaults();

builder.Services.AddHttpContextAccessor();

// EF Core persistence for conversation history (advisory-db).
builder.AddAdvisoryInfrastructure();

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

builder.Services.AddAdvisorRateLimiting();

// Shared Foundry chat-client pipeline (credential, history reducer, GenAI telemetry).
builder.Services.AddSingleton<IAdvisorChatClientFactory, FoundryChatClientFactory>();

// Per-turn MCP connection carrying the caller's bearer token.
builder.Services.AddSingleton<IAdvisorToolSource, McpAdvisorToolSource>();

// GenAI spans (model calls, latency, token usage) emitted by the chat-client
// pipeline; registering the source routes them to the Aspire dashboard via OTLP.
builder.Services.AddOpenTelemetry()
    .WithTracing(tracing => tracing.AddSource(FoundryChatClientFactory.TelemetrySourceName));

// Scoped: the chat service depends on the per-request DbContext.
builder.Services.AddScoped<AdvisorChatService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AdvisoryDbContext>().Database.Migrate();
}

if (authEnabled)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.UseRateLimiter();

app.MapDefaultEndpoints();
app.MapAdvisorEndpoints();
app.MapAdvisorHistoryEndpoints();

app.Run();
