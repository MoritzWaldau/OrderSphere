using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using OrderSphere.Webhooks.Application.DTOs;
using OrderSphere.Webhooks.Application.Features.Subscriptions.CreateSubscription;
using Xunit;

namespace OrderSphere.IntegrationTests.Api;

/// <summary>
/// Exercises the Webhooks Minimal-API surface through the real HTTP pipeline: routing, the
/// <c>RequireAuthorization()</c> gate, model binding, and the <c>Result&lt;T&gt;</c>→HTTP-status mapping.
/// </summary>
public sealed class WebhooksApiTests : IClassFixture<WebhooksApiFactory>
{
    private const string BaseUrl = "api/v1/webhooks";
    private readonly WebhooksApiFactory _factory;

    public WebhooksApiTests(WebhooksApiFactory factory) => _factory = factory;

    private HttpClient AuthedClient(string sub = "auth0|test-customer")
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.SubHeader, sub);
        return client;
    }

    private static object NewSubscription(string url = "https://example.test/hook") =>
        new { url, secret = "shhh", events = new[] { "OrderPlaced", "PaymentCompleted" } };

    [Fact]
    public async Task Anonymous_request_is_challenged_with_401()
    {
        var response = await _factory.CreateClient().GetAsync(BaseUrl);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Create_returns_201_with_location_and_persists_the_subscription()
    {
        var client = AuthedClient(sub: "auth0|create-flow");

        var create = await client.PostAsJsonAsync(BaseUrl, NewSubscription("https://example.test/created"));

        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = await create.Content.ReadFromJsonAsync<SubscriptionCreatedDto>();
        created.Should().NotBeNull();
        created!.Secret.Should().NotBeNullOrWhiteSpace();

        var fetched = await client.GetFromJsonAsync<SubscriptionDto>($"{BaseUrl}/{created.Id}");
        fetched.Should().NotBeNull();
        fetched!.Id.Should().Be(created.Id);
        fetched.Url.Should().Be("https://example.test/created");
        fetched.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Get_unknown_subscription_returns_404()
    {
        var response = await AuthedClient().GetAsync($"{BaseUrl}/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task List_returns_only_the_callers_subscriptions()
    {
        var owner = AuthedClient(sub: "auth0|list-owner");
        await owner.PostAsJsonAsync(BaseUrl, NewSubscription("https://example.test/owned-1"));
        await owner.PostAsJsonAsync(BaseUrl, NewSubscription("https://example.test/owned-2"));

        var stranger = AuthedClient(sub: "auth0|list-stranger");
        await stranger.PostAsJsonAsync(BaseUrl, NewSubscription("https://example.test/stranger"));

        var ownerList = await owner.GetFromJsonAsync<SubscriptionDto[]>(BaseUrl);

        ownerList.Should().NotBeNull();
        ownerList!.Should().HaveCount(2);
        ownerList.Select(s => s.Url).Should().BeEquivalentTo("https://example.test/owned-1", "https://example.test/owned-2");
    }

    [Fact]
    public async Task Deactivate_then_get_reflects_inactive_state()
    {
        var client = AuthedClient(sub: "auth0|deactivate-flow");
        var create = await client.PostAsJsonAsync(BaseUrl, NewSubscription());
        var created = await create.Content.ReadFromJsonAsync<SubscriptionCreatedDto>();

        var deactivate = await client.PostAsync($"{BaseUrl}/{created!.Id}/deactivate", content: null);
        deactivate.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var fetched = await client.GetFromJsonAsync<SubscriptionDto>($"{BaseUrl}/{created.Id}");
        fetched!.IsActive.Should().BeFalse();
    }
}
