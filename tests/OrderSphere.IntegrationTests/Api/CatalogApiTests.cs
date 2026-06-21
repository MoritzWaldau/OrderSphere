using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace OrderSphere.IntegrationTests.Api;

/// <summary>
/// Drives the Catalog API public and admin surfaces: anonymous browse endpoints, the
/// <c>CatalogAdminPolicy</c> role gate on the write groups, and the <c>Result&lt;T&gt;</c>→HTTP mapping.
/// </summary>
public sealed class CatalogApiTests : IClassFixture<CatalogApiFactory>
{
    private readonly CatalogApiFactory _factory;

    public CatalogApiTests(CatalogApiFactory factory) => _factory = factory;

    private HttpClient Client(string? sub = null, string? roles = null)
    {
        var client = _factory.CreateClient();
        if (sub is not null) client.DefaultRequestHeaders.Add(TestAuthHandler.SubHeader, sub);
        if (roles is not null) client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, roles);
        return client;
    }


    [Fact]
    public async Task Products_listing_is_anonymous_and_returns_a_paged_result()
    {
        using var doc = await _factory.CreateClient().GetFromJsonAsync<JsonDocument>("api/v1/products?page=1&pageSize=20");

        doc!.RootElement.GetProperty("items").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Unknown_slug_returns_404()
    {
        var response = await _factory.CreateClient().GetAsync("api/v1/products/does-not-exist");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Categories_listing_is_anonymous()
    {
        var response = await _factory.CreateClient().GetAsync("api/v1/categories");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Brands_listing_is_anonymous()
    {
        var response = await _factory.CreateClient().GetAsync("api/v1/brands");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }


    [Fact]
    public async Task Admin_products_challenges_anonymous_with_401()
    {
        var response = await _factory.CreateClient().GetAsync("api/v1/admin/products");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Admin_products_forbids_a_non_admin()
    {
        var response = await Client(sub: "auth0|plain", roles: "csr").GetAsync("api/v1/admin/products");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_products_allows_a_catalog_admin()
    {
        var response = await Client(sub: "auth0|admin", roles: "catalog-admin").GetAsync("api/v1/admin/products");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
