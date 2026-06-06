using System.Net.Http.Json;
using OrderSphere.Web.Models;

namespace OrderSphere.Web.Services;

public interface IAdminUserClient
{
    Task<List<AdminUserSummaryDto>> GetUsersAsync(CancellationToken ct = default);
    Task<AdminUserDetailDto?> GetUserByIdAsync(Guid id, CancellationToken ct = default);
}

public sealed class AdminUserClient : IAdminUserClient
{
    private readonly HttpClient _client;
    public AdminUserClient(HttpClient client) => _client = client;

    public async Task<List<AdminUserSummaryDto>> GetUsersAsync(CancellationToken ct = default)
    {
        var result = await _client.GetFromJsonAsync<List<AdminUserSummaryDto>>("/api/v1/admin/users", ct);
        return result ?? [];
    }

    public async Task<AdminUserDetailDto?> GetUserByIdAsync(Guid id, CancellationToken ct = default)
    {
        var response = await _client.GetAsync($"/api/v1/admin/users/{id}", ct);
        if (!response.IsSuccessStatusCode) return null;
        return await response.Content.ReadFromJsonAsync<AdminUserDetailDto>(ct);
    }
}
