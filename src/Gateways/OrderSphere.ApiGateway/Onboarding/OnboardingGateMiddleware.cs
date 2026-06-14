using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;

namespace OrderSphere.ApiGateway.Onboarding;

/// <summary>
/// Enforces the onboarding gate on all authenticated routes except the allow-list.
/// Runs after JWT authentication so <see cref="HttpContext.User"/> is populated.
/// Caches each user's onboarding status for <see cref="CacheTtl"/> to avoid a
/// UserProfile round-trip on every proxied request.
/// </summary>
public sealed class OnboardingGateMiddleware(RequestDelegate next)
{
    private static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(30);

    // Routes that must remain accessible regardless of onboarding status.
    // All subroutes of /api/v1/profile are included so the user can load their
    // profile, add an address, and call complete-onboarding without being blocked.
    private static bool IsAllowListed(PathString path) =>
        path.StartsWithSegments("/api/v1/profile") ||
        path.StartsWithSegments("/health");

    public async Task InvokeAsync(
        HttpContext ctx,
        IUserProfileStatusClient userProfileClient,
        IMemoryCache cache)
    {
        var isAuthenticated = ctx.User.Identity?.IsAuthenticated == true;
        var path = ctx.Request.Path;
        var isAllowListed = IsAllowListed(path);

        var sub = isAuthenticated
            ? ctx.User.FindFirst("sub")?.Value
              ?? ctx.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
            : null;

        // Track whether this request is the complete-onboarding call so we can
        // evict the cache after a successful response (3.9).
        var isCompleteOnboarding = sub is not null &&
            ctx.Request.Method == HttpMethods.Post &&
            path.StartsWithSegments("/api/v1/profile/complete-onboarding");

        if (isAllowListed || !isAuthenticated)
        {
            await next(ctx);

            if (isCompleteOnboarding && ctx.Response.StatusCode == StatusCodes.Status200OK)
                cache.Remove(CacheKey(sub!));

            return;
        }

        // Gated: check onboarding status (cached per sub).
        var cacheKey = CacheKey(sub!);
        if (!cache.TryGetValue<bool>(cacheKey, out var isComplete))
        {
            var bearerToken = ExtractBearer(ctx.Request.Headers.Authorization.ToString());
            isComplete = await userProfileClient.GetOnboardingStatusAsync(bearerToken, ctx.RequestAborted);
            cache.Set(cacheKey, isComplete, CacheTtl);
        }

        if (!isComplete)
        {
            ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
            await ctx.Response.WriteAsJsonAsync(
                new { error = "onboarding_required" },
                ctx.RequestAborted);
            return;
        }

        await next(ctx);
    }

    private static string CacheKey(string sub) => $"onboarding:{sub}";

    private static string ExtractBearer(string authHeader) =>
        authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authHeader["Bearer ".Length..]
            : authHeader;
}
