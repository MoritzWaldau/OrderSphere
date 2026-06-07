using Microsoft.AspNetCore.Authentication.JwtBearer;
using OrderSphere.Mcp.Server.Gateway;

var builder = WebApplication.CreateBuilder(args);

// Aspire defaults (OpenTelemetry, health checks, service discovery, resilience).
builder.AddServiceDefaults();

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<ICallerContext, HttpCallerContext>();

// Keycloak JWT validation. Authentication is optional at the transport level:
// public catalog tools work anonymously; user-scoped tools rely on the forwarded
// token to resolve data downstream. Audience is validated by the downstream
// services, not here, so the caller's existing token can be reused as-is.
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

// Typed client over the API Gateway's public surface; forwards the caller's bearer.
builder.Services.AddTransient<BearerForwardingHandler>();
builder.Services.AddHttpClient<IOrderSphereGateway, OrderSphereGateway>(client =>
{
    var gatewayUrl = builder.Configuration["Services:ApiGateway:BaseUrl"]
        ?? "http://ordersphere-apigateway";
    client.BaseAddress = new Uri(gatewayUrl);
}).AddHttpMessageHandler<BearerForwardingHandler>();

// MCP server over Streamable HTTP; tools discovered from this assembly.
builder.Services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

if (authEnabled)
{
    app.UseAuthentication();
    app.UseAuthorization();
}

app.MapDefaultEndpoints();
app.MapMcp("/mcp");

app.Run();
