using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using OrderSphere.BuildingBlocks.Security;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Resolves <see cref="ICurrentUser"/> from the ambient <see cref="IHttpContextAccessor"/>.
/// Registered as <em>scoped</em> so it is fresh per request and never shared across requests.
/// </summary>
internal sealed class HttpContextCurrentUser : ICurrentUser
{
    private readonly ClaimsPrincipal _principal;

    public HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _principal = httpContextAccessor.HttpContext?.User ?? new ClaimsPrincipal();
    }

    /// <inheritdoc/>
    public string? Sub =>
        _principal.FindFirstValue("sub") ??
        _principal.FindFirstValue(ClaimTypes.NameIdentifier);

    /// <inheritdoc/>
    public string? Name =>
        _principal.FindFirstValue("preferred_username") ??
        _principal.FindFirstValue(ClaimTypes.Name);

    /// <inheritdoc/>
    public string? Email =>
        _principal.FindFirstValue("email") ??
        _principal.FindFirstValue(ClaimTypes.Email);

    /// <inheritdoc/>
    public IReadOnlyList<string> Roles =>
        _principal.FindAll("roles").Select(c => c.Value).ToList().AsReadOnly();

    /// <inheritdoc/>
    public bool IsAuthenticated =>
        _principal.Identity?.IsAuthenticated == true;

    /// <inheritdoc/>
    public bool IsInRole(params string[] roles) =>
        roles.Any(_principal.IsInRole);
}

/// <summary>
/// Extension method to register <see cref="ICurrentUser"/> in the DI container.
/// Call from each API's composition root after calling <c>AddOrderSphereJwtAuth</c>.
/// </summary>
public static class CurrentUserExtensions
{
    /// <summary>
    /// Registers <see cref="ICurrentUser"/> as a scoped service backed by
    /// <see cref="HttpContextCurrentUser"/>.
    /// </summary>
    public static IServiceCollection AddCurrentUser(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
        return services;
    }
}
