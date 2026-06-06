namespace OrderSphere.Web.Auth;

/// <summary>
/// Scoped store for the XSRF request token returned by /bff/user.
/// BffAuthStateProvider writes the token after each /bff/user call;
/// AntiforgeryDelegatingHandler reads it and attaches X-XSRF-TOKEN
/// to every non-idempotent HTTP request.
/// In Blazor WASM, scoped services have effectively singleton lifetime
/// for the duration of the page session.
/// </summary>
public sealed class CsrfTokenService
{
    public string? Token { get; private set; }

    public void SetToken(string token) => Token = token;
}
