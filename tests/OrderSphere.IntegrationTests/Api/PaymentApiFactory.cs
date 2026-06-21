using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using OrderSphere.Payment.Api;
using OrderSphere.Payment.Infrastructure.Persistence;

namespace OrderSphere.IntegrationTests.Api;

/// <summary>
/// Boots the Payment API in-process with an in-memory database and the test auth scheme. The Service
/// Bus client is constructed lazily from a SAS connection string and its outbox dispatcher is removed
/// with the other hosted services.
/// </summary>
public sealed class PaymentApiFactory : WebApplicationFactory<ApiMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ApplyTestConfig();

        builder.ConfigureTestServices(services =>
        {
            services.UseInMemoryDb<PaymentDbContext>("payment-tests");
            services.AddTestAuthentication();
            services.RemoveHostedServices();
        });
    }
}
