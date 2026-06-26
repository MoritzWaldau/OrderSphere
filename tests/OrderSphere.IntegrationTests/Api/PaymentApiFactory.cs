using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using OrderSphere.Payment.Api;
using OrderSphere.Payment.Infrastructure.Persistence;

namespace OrderSphere.IntegrationTests.Api;

/// <summary>
/// Boots the Payment API in-process with an in-memory SQLite database and the test auth scheme. SQLite
/// is required (not EF InMemory) because <see cref="PaymentRecord"/> maps <c>Money</c> as a
/// ComplexProperty, which the non-relational InMemory provider cannot materialise. The Service Bus
/// client is constructed lazily from a SAS connection string and its outbox dispatcher is removed with
/// the other hosted services.
/// </summary>
public sealed class PaymentApiFactory : WebApplicationFactory<ApiMarker>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ApplyTestConfig();

        builder.ConfigureTestServices(services =>
        {
            services.AddTestAuthentication();
            // Strip outbox/background services before UseSqliteDb adds the schema initializer,
            // which must survive as the sole background service.
            services.RemoveHostedServices();
            services.UseSqliteDb<PaymentDbContext>(); // relational: PaymentRecord projects Money via ComplexProperty
        });
    }
}
