using OrderSphere.Web.Models;

namespace OrderSphere.Web.Services;

public interface IAdminUserClient
{
    Task<ApiResult<List<AdminUserSummaryDto>>> GetUsersAsync(CancellationToken ct = default);
    Task<ApiResult<AdminUserDetailDto>> GetUserByIdAsync(Guid id, CancellationToken ct = default);
}

public sealed class AdminUserClient(HttpClient client) : IAdminUserClient
{
    public Task<ApiResult<List<AdminUserSummaryDto>>> GetUsersAsync(CancellationToken ct = default)
        => client.GetApiAsync<List<AdminUserSummaryDto>>("/api/v1/admin/users", ct);

    public Task<ApiResult<AdminUserDetailDto>> GetUserByIdAsync(Guid id, CancellationToken ct = default)
        => client.GetApiAsync<AdminUserDetailDto>($"/api/v1/admin/users/{id}", ct);
}
