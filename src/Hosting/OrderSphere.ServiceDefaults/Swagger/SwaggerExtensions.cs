using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Microsoft.Extensions.Hosting;

public static class SwaggerExtensions
{
    public static IHostApplicationBuilder AddOrderSphereSwagger(
        this IHostApplicationBuilder builder,
        string title,
        string docId = "v1")
    {
        var authority = builder.Configuration["Oidc:Authority"]
            ?? "https://ordersphere-dev.eu.auth0.com/";

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(docId, new OpenApiInfo { Title = title, Version = docId });

            options.OperationFilter<RemoveVersionFromParameter>();
            options.DocumentFilter<ReplaceVersionWithExactValueInPath>();

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

            options.CustomSchemaIds(t => t.FullName!.Replace("+", "."));
        });

        return builder;
    }

    /// <summary>
    /// Maps Swagger JSON + Swagger UI endpoints. Must be called inside an
    /// <c>if (app.Environment.IsDevelopment())</c> block.
    /// </summary>
    public static WebApplication UseOrderSphereSwagger(
        this WebApplication app,
        string docId = "v1",
        string docTitle = "API")
    {
        app.UseSwagger();
        app.UseSwaggerUI(opt =>
        {
            opt.SwaggerEndpoint($"/swagger/{docId}/swagger.json", docTitle);
            opt.OAuthClientId("swagger-ui");
            opt.OAuthUsePkce();
            opt.OAuthScopes("openid", "profile");
            opt.OAuthAdditionalQueryStringParams(new Dictionary<string, string>
            {
                ["audience"] = "https://api.ordersphere.dev"
            });
        });
        return app;
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
        var version = swaggerDoc.Info.Version!.TrimStart('v');
        var updatedPaths = new OpenApiPaths();
        foreach (var (path, item) in swaggerDoc.Paths)
            updatedPaths.Add(path.Replace("{version}", version), item);
        swaggerDoc.Paths = updatedPaths;
    }
}
