using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace OrderSphere.ApiGateway.Onboarding;

public sealed class UserProfileStatusClient(HttpClient httpClient) : IUserProfileStatusClient
{
    public async Task<bool> GetOnboardingStatusAsync(string bearerToken, CancellationToken ct = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/profile/onboarding-status");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            var response = await httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) return false;

            // Response body is a raw JSON boolean: true or false
            return await response.Content.ReadFromJsonAsync<bool>(ct);
        }
        catch
        {
            return false;
        }
    }
}
