using OrderSphere.UI.Models.Auth;

namespace OrderSphere.UI.Services.Auth;

public interface ICurrentUserService
{
    event Action? OnCurrentUserChanged;

    Task<CurrentUserInfo> GetCurrentUserInfoAsync();
    Task<string?> GetUserIdAsync();
    Task<Guid?> GetCustomerIdAsync();
    Task<string?> GetUserNameAsync();
    Task<string?> GetEmailAsync();
    Task<bool> IsAuthenticatedAsync();
}