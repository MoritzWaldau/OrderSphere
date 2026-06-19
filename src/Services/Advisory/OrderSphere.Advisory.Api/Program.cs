using Microsoft.EntityFrameworkCore;
using OrderSphere.Advisory.Api.Agent;
using OrderSphere.Advisory.Api.Configuration;
using OrderSphere.Advisory.Api.Endpoints;
using OrderSphere.Advisory.Infrastructure;
using OrderSphere.Advisory.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

// Aspire defaults (OpenTelemetry, health checks, service discovery, resilience).
builder.AddServiceDefaults();
builder.AddOrderSphereSwagger("OrderSphere Advisory API");

// EF Core persistence for conversation history (advisory-db).
builder.AddAdvisoryInfrastructure();

// Auth0 JWT validation. The end-user token is forwarded by the BFF; the agent
// passes it on to the MCP server. Audience is validated downstream, not here —
// hence the no-audience overload (ValidateAudience = false).
builder.AddOrderSphereJwtAuth();
builder.Services.AddCurrentUser();

builder.AddOrderSphereExceptionHandling();
builder.Services.AddAdvisoryApiVersioning();
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

if (app.Environment.IsDevelopment())
{
    app.UseOrderSphereSwagger(docTitle: "OrderSphere Advisory API");
}

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapDefaultEndpoints();
app.MapAdvisorEndpoints();

app.Run();
