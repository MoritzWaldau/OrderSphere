using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace OrderSphere.Catalog.Api.Configuration;

/// <summary>
/// Registers Swagger documents for each API version and installs path-cleanup filters
/// so that the {version:apiVersion} route constraint is replaced with the concrete value
/// in the generated OpenAPI spec.
/// </summary>
public sealed class ConfigureSwaggerOptions(IConfiguration configuration)
    : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "OrderSphere Catalog API",
            Version = "v1",
            Description = "REST API for product and category catalog management."
        });

        // Remove the {version} path parameter from every operation — it is not a
        // caller-supplied value; it is fixed per document.
        options.OperationFilter<RemoveVersionFromParameter>();

        // Substitute {version} in path strings with the concrete version number
        // taken from the document's Info.Version (e.g. "v1" → paths use "1").
        options.DocumentFilter<ReplaceVersionWithExactValueInPath>();

        options.CustomSchemaIds(type => type.FullName!.Replace("+", "."));

        // OAuth2 Authorization Code + PKCE — enables token acquisition directly
        // from Swagger UI via the Swagger UI Auth0 application.
        var authority = configuration["Oidc:Authority"]
            ?? "https://ordersphere-dev.eu.auth0.com/";

        options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.OAuth2,
            Flows = new OpenApiOAuthFlows
            {
                AuthorizationCode = new OpenApiOAuthFlow
                {
                    AuthorizationUrl = new Uri($"{authority.TrimEnd('/')}/authorize"),
                    TokenUrl = new Uri($"{authority.TrimEnd('/')}/oauth/token"),
                    Scopes = new Dictionary<string, string>
                    {
                        ["openid"] = "OpenID Connect identity token",
                        ["profile"] = "Basic user profile (name, email)"
                    }
                }
            }
        });

        options.AddSecurityRequirement(_ => new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecuritySchemeReference("oauth2"),
                ["openid", "profile"]
            }
        });
    }
}

file sealed class RemoveVersionFromParameter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var versionParam = operation.Parameters?
            .FirstOrDefault(p => p.Name == "version");

        if (versionParam is not null)
            operation.Parameters!.Remove(versionParam);
    }
}

file sealed class ReplaceVersionWithExactValueInPath : IDocumentFilter
{
    public void Apply(OpenApiDocument swaggerDoc, DocumentFilterContext context)
    {
        // "v1" → "1"  (strips the leading "v" so that /api/v{version}/… → /api/v1/…)
        var version = swaggerDoc.Info.Version!.TrimStart('v');

        var updatedPaths = new OpenApiPaths();
        foreach (var (path, item) in swaggerDoc.Paths)
            updatedPaths.Add(path.Replace("{version}", version), item);

        swaggerDoc.Paths = updatedPaths;
    }
}
