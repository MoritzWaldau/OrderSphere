using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace Microsoft.Extensions.Hosting;

public static class SwaggerExtensions
{
    /// <summary>
    /// Registers Swashbuckle with an OAuth2 Authorization Code + PKCE security scheme
    /// pointing at the Keycloak realm configured under <c>Keycloak:Authority</c>.
    /// Intended for use in non-versioned API services. Catalog.Api uses its own versioned setup.
    /// </summary>
    public static IHostApplicationBuilder AddOrderSphereSwagger(
        this IHostApplicationBuilder builder,
        string title,
        string docId = "v1")
    {
        var authority = builder.Configuration["Keycloak:Authority"]
            ?? "http://localhost:8080/realms/ordersphere";

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc(docId, new OpenApiInfo { Title = title, Version = docId });

            options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Flows = new OpenApiOAuthFlows
                {
                    AuthorizationCode = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new Uri($"{authority}/protocol/openid-connect/auth"),
                        TokenUrl         = new Uri($"{authority}/protocol/openid-connect/token"),
                        Scopes = new Dictionary<string, string>
                        {
                            ["openid"]  = "OpenID Connect identity token",
                            ["profile"] = "Basic user profile (name, preferred_username)",
                            ["roles"]   = "Realm roles claim"
                        }
                    }
                }
            });

            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                            { Type = ReferenceType.SecurityScheme, Id = "oauth2" }
                    },
                    ["openid", "profile", "roles"]
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
        string docId    = "v1",
        string docTitle = "API")
    {
        app.UseSwagger();
        app.UseSwaggerUI(opt =>
        {
            opt.SwaggerEndpoint($"/swagger/{docId}/swagger.json", docTitle);
            opt.OAuthClientId("swagger-ui");
            opt.OAuthUsePkce();
            opt.OAuthScopes("openid", "profile", "roles");
        });
        return app;
    }
}
