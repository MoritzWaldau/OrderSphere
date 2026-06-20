using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using OrderSphere.Webhooks.Api;
using OrderSphere.Webhooks.Infrastructure.Persistence;

namespace OrderSphere.IntegrationTests.Api;

/// <summary>
/// Boots the Webhooks API in-process with an in-memory database and the header-driven test auth scheme.
/// </summary>
public sealed class WebhooksApiFactory : WebApplicationFactory<ApiMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ApplyTestConfig();

        builder.ConfigureTestServices(services =>
        {
            services.UseInMemoryDb<WebhooksDbContext>("webhooks-tests");
            services.AddTestAuthentication();
        });
    }
}
