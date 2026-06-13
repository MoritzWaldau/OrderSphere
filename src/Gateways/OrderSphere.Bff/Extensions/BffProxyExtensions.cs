using Microsoft.AspNetCore.Authentication;
using Yarp.ReverseProxy.Transforms;

namespace OrderSphere.Bff.Extensions;

internal static class BffProxyExtensions
{
    /// <summary>
    /// Configures YARP reverse proxy. Attaches the user's Bearer access token to all
    /// forwarded requests so downstream services can validate JWT identity.
    /// </summary>
    public static void AddBffProxy(this WebApplicationBuilder builder)
    {
        builder.Services.AddReverseProxy()
            .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
            .AddServiceDiscoveryDestinationResolver()
            .AddTransforms(transforms =>
            {
                transforms.AddRequestTransform(async ctx =>
                {
                    var token = await ctx.HttpContext.GetTokenAsync("access_token");
                    if (!string.IsNullOrEmpty(token))
                    {
                        ctx.ProxyRequest.Headers.Authorization =
                            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                    }
                });
            });
    }
}
