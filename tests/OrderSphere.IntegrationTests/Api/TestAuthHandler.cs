using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace OrderSphere.IntegrationTests.Api;

/// <summary>
/// Replaces the production JWT Bearer scheme during integration tests. A request is authenticated
/// when it carries an <c>X-Test-Sub</c> header (the OIDC <c>sub</c> claim); roles are supplied via a
/// comma-separated <c>X-Test-Roles</c> header. A request with no <c>X-Test-Sub</c> stays anonymous,
/// so endpoints guarded by <c>RequireAuthorization()</c> challenge with 401 exactly as in production.
/// </summary>
internal sealed class TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    public const string SchemeName = "Test";

    /// <summary>Matches <c>Oidc:RolesClaim</c> so that role-based policies resolve identically to production.</summary>
    public const string RolesClaimType = "https://ordersphere.dev/roles";

    public const string SubHeader = "X-Test-Sub";
    public const string RolesHeader = "X-Test-Roles";

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(SubHeader, out var sub) || string.IsNullOrWhiteSpace(sub))
            return Task.FromResult(AuthenticateResult.NoResult());

        var claims = new List<Claim> { new("sub", sub!), new("name", "Test User") };

        if (Request.Headers.TryGetValue(RolesHeader, out var roles))
        {
            foreach (var role in roles.ToString().Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                // RolesClaimType drives IsInRole/RequireRole policies; the short "roles" claim
                // backs ICurrentUser.Roles. Production tokens carry both, so emit both here.
                claims.Add(new Claim(RolesClaimType, role));
                claims.Add(new Claim("roles", role));
            }
        }

        var identity = new ClaimsIdentity(claims, SchemeName, nameType: "name", roleType: RolesClaimType);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}
