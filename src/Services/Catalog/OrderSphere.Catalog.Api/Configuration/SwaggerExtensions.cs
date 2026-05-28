using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace OrderSphere.Catalog.Api.Configuration;

public static class SwaggerExtensions
{
    public static IServiceCollection AddCatalogSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddTransient<IConfigureOptions<SwaggerGenOptions>, ConfigureSwaggerOptions>();
        services.AddSwaggerGen();
        return services;
    }

    public static WebApplication UseCatalogSwagger(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalog API v1");
            options.OAuthClientId("swagger-ui");
            options.OAuthUsePkce();
            options.OAuthScopes("openid", "profile", "roles");
        });
        return app;
    }
}
