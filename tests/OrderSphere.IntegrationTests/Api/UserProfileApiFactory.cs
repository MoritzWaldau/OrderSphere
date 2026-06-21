using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using OrderSphere.UserProfile.Api;
using OrderSphere.UserProfile.Infrastructure.Persistence;

namespace OrderSphere.IntegrationTests.Api;

/// <summary>
/// Boots the UserProfile API in-process with an in-memory database and the test auth scheme.
/// </summary>
public sealed class UserProfileApiFactory : WebApplicationFactory<ApiMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ApplyTestConfig();

        builder.ConfigureTestServices(services =>
        {
            services.UseInMemoryDb<UserProfileDbContext>("userprofile-tests");
            services.AddTestAuthentication();
        });
    }
}
