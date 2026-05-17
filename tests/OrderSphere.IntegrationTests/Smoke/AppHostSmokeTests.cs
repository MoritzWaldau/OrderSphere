using System.Net;
using Aspire.Hosting;
using Aspire.Hosting.Testing;

namespace OrderSphere.IntegrationTests.Smoke;

[Collection(nameof(AspireCollection))]
public sealed class AppHostSmokeTests(AspireFixture fixture)
{
    [Fact]
    public async Task Ui_RespondsToRoot()
    {
        var client = fixture.App.CreateHttpClient("ordersphere-ui");

        var response = await client.GetAsync("/");

        response.StatusCode.Should().BeOneOf(HttpStatusCode.OK, HttpStatusCode.Redirect, HttpStatusCode.Found);
    }

    [Fact]
    public async Task ServiceBus_ConnectionStringIsAvailable()
    {
        var connectionString = await fixture.App.GetConnectionStringAsync("azure-service-bus");

        connectionString.Should().NotBeNullOrWhiteSpace();
    }
}
