using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace OrderSphere.Web.Auth;

/// <summary>
/// Retrieves auth state from the BFF's /bff/user endpoint.
/// The BFF validates the session cookie server-side and returns user claims.
/// The browser never touches Auth0 directly.
/// </summary>
public sealed class BffAuthStateProvider : AuthenticationStateProvider
{
    private readonly HttpClient _client;
    private readonly CsrfTokenService _csrfTokenService;
    private static readonly AuthenticationState _anonymous =
        new(new ClaimsPrincipal(new ClaimsIdentity()));

    public BffAuthStateProvider(HttpClient client, CsrfTokenService csrfTokenService)
    {
        _client = client;
        _csrfTokenService = csrfTokenService;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var userInfo = await _client.GetFromJsonAsync<UserInfoDto>("/bff/user");

            if (userInfo?.IsAuthenticated != true)
            {
                if (!string.IsNullOrEmpty(userInfo?.XsrfToken))
                    _csrfTokenService.SetToken(userInfo.XsrfToken);
                return _anonymous;
            }

            if (!string.IsNullOrEmpty(userInfo.XsrfToken))
                _csrfTokenService.SetToken(userInfo.XsrfToken);

            var claims = new List<Claim>();

            if (!string.IsNullOrWhiteSpace(userInfo.Sub))
                claims.Add(new Claim(ClaimTypes.NameIdentifier, userInfo.Sub));

            if (!string.IsNullOrWhiteSpace(userInfo.Name))
                claims.Add(new Claim(ClaimTypes.Name, userInfo.Name));

            if (!string.IsNullOrWhiteSpace(userInfo.Email))
                claims.Add(new Claim(ClaimTypes.Email, userInfo.Email));

            foreach (var role in userInfo.Roles ?? [])
                claims.Add(new Claim("roles", role));

            claims.Add(new Claim("onboardingComplete", userInfo.OnboardingComplete ? "true" : "false"));

            var identity = new ClaimsIdentity(claims, authenticationType: "BffCookie",
                nameType: ClaimTypes.Name, roleType: "roles");
            return new AuthenticationState(new ClaimsPrincipal(identity));
        }
        catch
        {
            return _anonymous;
        }
    }

    public void NotifyAuthStateChanged()
        => NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
}
