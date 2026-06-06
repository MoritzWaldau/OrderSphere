using System.Net.Http.Json;
using OrderSphere.Web.Models;

namespace OrderSphere.Web.Services;

public interface IUserProfileClient
{
    Task<ProfileDto?> GetOrCreateProfileAsync(CancellationToken ct = default);
    Task<ProfileDto?> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct = default);
    Task UpdatePreferencesAsync(UpdatePreferencesRequest request, CancellationToken ct = default);
    Task<List<AddressDto>> GetAddressesAsync(CancellationToken ct = default);
    Task<AddressDto?> AddAddressAsync(CreateAddressRequest request, CancellationToken ct = default);
    Task DeleteAddressAsync(Guid addressId, CancellationToken ct = default);
    Task SetDefaultAddressAsync(Guid addressId, CancellationToken ct = default);
}

public sealed class UserProfileClient : IUserProfileClient
{
    private readonly HttpClient _client;

    public UserProfileClient(HttpClient client) => _client = client;

    public async Task<ProfileDto?> GetOrCreateProfileAsync(CancellationToken ct = default)
    {
        var response = await _client.GetAsync("/api/v1/profile", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ProfileDto>(ct);
    }

    public async Task<ProfileDto?> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct = default)
    {
        var response = await _client.PutAsJsonAsync("/api/v1/profile", request, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<ProfileDto>(ct);
    }

    public async Task UpdatePreferencesAsync(UpdatePreferencesRequest request, CancellationToken ct = default)
        => await _client.PutAsJsonAsync("/api/v1/profile/preferences", request, ct);

    public async Task<List<AddressDto>> GetAddressesAsync(CancellationToken ct = default)
    {
        var result = await _client.GetFromJsonAsync<List<AddressDto>>("/api/v1/profile/addresses", ct);
        return result ?? [];
    }

    public async Task<AddressDto?> AddAddressAsync(CreateAddressRequest request, CancellationToken ct = default)
    {
        var response = await _client.PostAsJsonAsync("/api/v1/profile/addresses", request, ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<AddressDto>(ct);
    }

    public async Task DeleteAddressAsync(Guid addressId, CancellationToken ct = default)
        => await _client.DeleteAsync($"/api/v1/profile/addresses/{addressId}", ct);

    public async Task SetDefaultAddressAsync(Guid addressId, CancellationToken ct = default)
        => await _client.PostAsync($"/api/v1/profile/addresses/{addressId}/set-default", null, ct);
}
