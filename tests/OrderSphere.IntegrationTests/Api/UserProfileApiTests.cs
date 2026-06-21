using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using OrderSphere.UserProfile.Application.Models;
using Xunit;

namespace OrderSphere.IntegrationTests.Api;

/// <summary>
/// Drives the UserProfile Minimal-API surface end-to-end: the <c>RequireAuthorization()</c> gate,
/// the <c>AdminPolicy</c> role check, profile auto-provisioning, and the address sub-resource.
/// </summary>
public sealed class UserProfileApiTests : IClassFixture<UserProfileApiFactory>
{
    private readonly UserProfileApiFactory _factory;

    public UserProfileApiTests(UserProfileApiFactory factory) => _factory = factory;

    private HttpClient Client(string sub = "auth0|profile-user", string? roles = null)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.SubHeader, sub);
        if (roles is not null)
            client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, roles);
        return client;
    }

    [Fact]
    public async Task Anonymous_request_is_challenged_with_401()
    {
        var response = await _factory.CreateClient().GetAsync("api/v1/profile");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_profile_provisions_one_for_the_authenticated_subject()
    {
        var client = Client(sub: "auth0|ensure-flow");

        var profile = await client.GetFromJsonAsync<ProfileDto>("api/v1/profile");

        profile.Should().NotBeNull();
        profile!.Subject.Should().Be("auth0|ensure-flow");
    }

    [Fact]
    public async Task Add_address_returns_201_and_appears_in_the_profile()
    {
        var client = Client(sub: "auth0|address-flow");
        await client.GetAsync("api/v1/profile"); // ensure the profile exists first

        var request = new CreateAddressRequest(
            "Home", "Erika", "Mustermann", "Hauptstraße 1", "Berlin", "10115", "Deutschland", SetAsDefault: true);

        var create = await client.PostAsJsonAsync("api/v1/profile/addresses", request);
        create.StatusCode.Should().Be(HttpStatusCode.Created);

        var addresses = await client.GetFromJsonAsync<AddressDto[]>("api/v1/profile/addresses");
        addresses.Should().ContainSingle(a => a.Label == "Home" && a.IsDefault);
    }

    [Fact]
    public async Task Admin_endpoint_forbids_a_non_admin()
    {
        var response = await Client(sub: "auth0|plain-user").GetAsync("api/v1/admin/users");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_endpoint_allows_an_admin()
    {
        var response = await Client(sub: "auth0|admin-user", roles: "admin").GetAsync("api/v1/admin/users");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
