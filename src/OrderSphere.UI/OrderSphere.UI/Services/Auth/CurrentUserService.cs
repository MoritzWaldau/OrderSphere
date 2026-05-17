using Microsoft.AspNetCore.Components.Authorization;
using OrderSphere.UI.Models.Auth;
using System.Security.Claims;

namespace OrderSphere.UI.Services.Auth;

public sealed class CurrentUserService : ICurrentUserService, IDisposable
{
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private CurrentUserInfo? _cachedUserInfo;
    private bool _isLoaded;

    public event Action? OnCurrentUserChanged;

    public CurrentUserService(AuthenticationStateProvider authenticationStateProvider)
    {
        _authenticationStateProvider = authenticationStateProvider;
        _authenticationStateProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;
    }

    public async Task<CurrentUserInfo> GetCurrentUserInfoAsync()
    {
        if (_isLoaded && _cachedUserInfo is not null)
            return _cachedUserInfo;

        var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        _cachedUserInfo = MapUser(authState.User);
        _isLoaded = true;

        return _cachedUserInfo;
    }

    public async Task<string?> GetUserIdAsync()
    {
        var userInfo = await GetCurrentUserInfoAsync();
        return userInfo.UserId;
    }

    public async Task<Guid?> GetCustomerIdAsync()
    {
        var userInfo = await GetCurrentUserInfoAsync();
        return userInfo.CustomerId;
    }

    public async Task<string?> GetUserNameAsync()
    {
        var userInfo = await GetCurrentUserInfoAsync();
        return userInfo.UserName;
    }

    public async Task<string?> GetEmailAsync()
    {
        var userInfo = await GetCurrentUserInfoAsync();
        return userInfo.Email;
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var userInfo = await GetCurrentUserInfoAsync();
        return userInfo.IsAuthenticated;
    }

    private void OnAuthenticationStateChanged(Task<AuthenticationState> task)
    {
        _cachedUserInfo = null;
        _isLoaded = false;
        OnCurrentUserChanged?.Invoke();
    }

    private static CurrentUserInfo MapUser(ClaimsPrincipal user)
    {
        var isAuthenticated = user.Identity?.IsAuthenticated == true;

        if (!isAuthenticated)
        {
            return new CurrentUserInfo
            {
                IsAuthenticated = false
            };
        }


        var userId = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? throw new InvalidOperationException("User ID claim (NameIdentifier) is missing from ClaimsPrincipal.");

        var firstName = user.FindFirst("FirstName")?.Value
            ?? throw new InvalidOperationException("FirstName claim is missing from ClaimsPrincipal.");

        var lastName = user.FindFirst("LastName")?.Value
            ?? throw new InvalidOperationException("LastName claim is missing from ClaimsPrincipal.");

        var fullName = $"{firstName} {lastName}";

        var email = user.FindFirst(ClaimTypes.Email)?.Value
            ?? throw new InvalidOperationException("Email claim is missing from ClaimsPrincipal.");

        return new CurrentUserInfo
        {
            IsAuthenticated = true,
            UserId = userId,
            UserName = fullName,
            Email = email
        };
    }

    public void Dispose()
    {
        _authenticationStateProvider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
    }
}
