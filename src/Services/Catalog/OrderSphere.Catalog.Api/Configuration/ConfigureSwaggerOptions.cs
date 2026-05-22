using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace OrderSphere.Catalog.Api.Configuration;

/// <summary>
/// Registers Swagger documents for each API version.
/// Currently hardcoded to v1. When additional versions are introduced,
/// add corresponding SwaggerDoc calls here.
/// </summary>
public sealed class ConfigureSwaggerOptions : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "OrderSphere Catalog API",
            Version = "v1",
            Description = "REST API for product and category catalog management."
        });

        options.CustomSchemaIds(type => type.FullName!.Replace("+", "."));
    }
}
