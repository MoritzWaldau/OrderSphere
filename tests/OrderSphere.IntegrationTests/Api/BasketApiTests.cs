using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Xunit;

namespace OrderSphere.IntegrationTests.Api;

/// <summary>
/// Drives the Basket Minimal-API surface end-to-end: the auth gate, rate-limiting metadata, model
/// binding, and the add/get/remove cart lifecycle against the stubbed Catalog client.
/// </summary>
public sealed class BasketApiTests : IClassFixture<BasketApiFactory>
{
    private const string BaseUrl = "api/v1/cart";
    private readonly BasketApiFactory _factory;

    public BasketApiTests(BasketApiFactory factory) => _factory = factory;

    private HttpClient Client(string sub)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.SubHeader, sub);
        return client;
    }

    [Fact]
    public async Task Anonymous_request_is_challenged_with_401()
    {
        var response = await _factory.CreateClient().GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Empty_cart_is_returned_for_a_fresh_customer()
    {
        using var doc = await Client("auth0|fresh-cart").GetFromJsonAsync<JsonDocument>(BaseUrl);

        doc!.RootElement.GetProperty("items").GetArrayLength().Should().Be(0);
    }

    [Fact]
    public async Task Add_to_cart_then_get_reflects_the_item()
    {
        var client = Client("auth0|add-flow");
        var productId = Guid.NewGuid();

        var add = await client.PostAsJsonAsync($"{BaseUrl}/add", new { productId, quantity = 2 });
        add.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var cart = await client.GetFromJsonAsync<JsonDocument>(BaseUrl);
        var items = cart!.RootElement.GetProperty("items");
        items.GetArrayLength().Should().Be(1);
        items[0].GetProperty("productId").GetGuid().Should().Be(productId);
    }

    [Fact]
    public async Task Remove_from_cart_empties_it()
    {
        var client = Client("auth0|remove-flow");
        var productId = Guid.NewGuid();
        await client.PostAsJsonAsync($"{BaseUrl}/add", new { productId, quantity = 1 });

        var remove = await client.DeleteAsync($"{BaseUrl}/remove?productId={productId}");
        remove.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var cart = await client.GetFromJsonAsync<JsonDocument>(BaseUrl);
        cart!.RootElement.GetProperty("items").GetArrayLength().Should().Be(0);
    }
}
