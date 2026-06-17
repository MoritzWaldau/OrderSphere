using System.Net.Http.Json;
using OrderSphere.Web.Models;

namespace OrderSphere.Web.Services;

public interface IUserProfileClient
{
    Task<ApiResult<ProfileDto>> GetOrCreateProfileAsync(CancellationToken ct = default);
    Task<ApiResult<ProfileDto>> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct = default);
    Task<ApiResult> UpdatePreferencesAsync(UpdatePreferencesRequest request, CancellationToken ct = default);
    Task<ApiResult<List<AddressDto>>> GetAddressesAsync(CancellationToken ct = default);
    Task<ApiResult<AddressDto>> AddAddressAsync(CreateAddressRequest request, CancellationToken ct = default);
    Task<ApiResult> DeleteAddressAsync(Guid addressId, CancellationToken ct = default);
    Task<ApiResult> SetDefaultAddressAsync(Guid addressId, CancellationToken ct = default);
    Task<ApiResult<ProfileDto>> CompleteOnboardingAsync(CancellationToken ct = default);
}

public sealed class UserProfileClient(HttpClient client) : IUserProfileClient
{
    public Task<ApiResult<ProfileDto>> GetOrCreateProfileAsync(CancellationToken ct = default)
        => client.GetApiAsync<ProfileDto>("/api/v1/profile", ct);

    public Task<ApiResult<ProfileDto>> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct = default)
        => client.SendApiAsync<ProfileDto>(
            new HttpRequestMessage(HttpMethod.Put, "/api/v1/profile") { Content = JsonContent.Create(request) }, ct);

    public Task<ApiResult> UpdatePreferencesAsync(UpdatePreferencesRequest request, CancellationToken ct = default)
        => client.SendApiAsync(
            new HttpRequestMessage(HttpMethod.Put, "/api/v1/profile/preferences") { Content = JsonContent.Create(request) }, ct);

    public Task<ApiResult<List<AddressDto>>> GetAddressesAsync(CancellationToken ct = default)
        => client.GetApiAsync<List<AddressDto>>("/api/v1/profile/addresses", ct);

    public Task<ApiResult<AddressDto>> AddAddressAsync(CreateAddressRequest request, CancellationToken ct = default)
        => client.SendApiAsync<AddressDto>(
            new HttpRequestMessage(HttpMethod.Post, "/api/v1/profile/addresses") { Content = JsonContent.Create(request) }, ct);

    public Task<ApiResult> DeleteAddressAsync(Guid addressId, CancellationToken ct = default)
        => client.SendApiAsync(
            new HttpRequestMessage(HttpMethod.Delete, $"/api/v1/profile/addresses/{addressId}"), ct);

    public Task<ApiResult> SetDefaultAddressAsync(Guid addressId, CancellationToken ct = default)
        => client.SendApiAsync(
            new HttpRequestMessage(HttpMethod.Post, $"/api/v1/profile/addresses/{addressId}/set-default"), ct);

    public Task<ApiResult<ProfileDto>> CompleteOnboardingAsync(CancellationToken ct = default)
        => client.SendApiAsync<ProfileDto>(
            new HttpRequestMessage(HttpMethod.Post, "/api/v1/profile/complete-onboarding"), ct);
}
