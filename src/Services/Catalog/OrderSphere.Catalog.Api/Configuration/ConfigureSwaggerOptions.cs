using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace OrderSphere.Catalog.Api.Configuration;

public sealed class ConfigureSwaggerOptions(IApiVersionDescriptionProvider provider)
    : IConfigureOptions<SwaggerGenOptions>
{
    public void Configure(SwaggerGenOptions options)
    {
        foreach (var description in provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(description.GroupName, new OpenApiInfo
            {
                Title = "OrderSphere Catalog API",
                Version = description.ApiVersion.ToString(),
                Description = description.IsDeprecated
                    ? "Deprecated API version."
                    : "REST API for product and category catalog management."
            });
        }
    }
}
