using System.Net;
using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using OrderSphere.ApiGateway.Onboarding;
using Xunit;

namespace OrderSphere.ApiGateway.Tests;

public sealed class OnboardingGateMiddlewareTests
{
    private const string TestSub = "auth0|test-sub-001";

    private static (OnboardingGateMiddleware middleware, IUserProfileStatusClient client, IMemoryCache cache)
        CreateSut(RequestDelegate? next = null)
    {
        var requestDelegate = next ?? (_ => Task.CompletedTask);
        var middleware = new OnboardingGateMiddleware(requestDelegate);
        var client = Substitute.For<IUserProfileStatusClient>();
        var cache = new MemoryCache(new MemoryCacheOptions());
        return (middleware, client, cache);
    }

    private static HttpContext AuthenticatedContext(string path, string method = "GET")
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = path;
        ctx.Request.Method = method;
        ctx.Request.Headers.Authorization = "Bearer test-token";
        ctx.Response.Body = new System.IO.MemoryStream();

        var claims = new[]
        {
            new Claim("sub", TestSub),
            new Claim(ClaimTypes.NameIdentifier, TestSub),
        };
        ctx.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "Bearer"));
        return ctx;
    }

    private static HttpContext AnonymousContext(string path, string method = "GET")
    {
        var ctx = new DefaultHttpContext();
        ctx.Request.Path = path;
        ctx.Request.Method = method;
        ctx.Response.Body = new System.IO.MemoryStream();
        return ctx;
    }

    // ── Unauthenticated pass-through ─────────────────────────────────────────

    [Fact]
    public async Task Invoke_Unauthenticated_PassesThrough()
    {
        var nextCalled = false;
        var (middleware, client, cache) = CreateSut(_ => { nextCalled = true; return Task.CompletedTask; });
        var ctx = AnonymousContext("/api/v1/catalog/products");

        await middleware.InvokeAsync(ctx, client, cache);

        nextCalled.Should().BeTrue();
        await client.DidNotReceive().GetOnboardingStatusAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ── Allow-listed paths ────────────────────────────────────────────────────

    [Theory]
    [InlineData("/api/v1/profile")]
    [InlineData("/api/v1/profile/onboarding-status")]
    [InlineData("/api/v1/profile/complete-onboarding")]
    [InlineData("/api/v1/profile/addresses")]
    [InlineData("/health")]
    [InlineData("/health/gateway")]
    public async Task Invoke_AllowListedPath_PassesThroughWithoutCheck(string path)
    {
        var nextCalled = false;
        var (middleware, client, cache) = CreateSut(_ => { nextCalled = true; return Task.CompletedTask; });
        var ctx = AuthenticatedContext(path);

        await middleware.InvokeAsync(ctx, client, cache);

        nextCalled.Should().BeTrue();
        await client.DidNotReceive().GetOnboardingStatusAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ── Onboarding gate: not complete ─────────────────────────────────────────

    [Fact]
    public async Task Invoke_AuthenticatedNotComplete_Returns403()
    {
        var (middleware, client, cache) = CreateSut();
        client.GetOnboardingStatusAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        var ctx = AuthenticatedContext("/api/v1/catalog/products");

        await middleware.InvokeAsync(ctx, client, cache);

        ctx.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
    }

    [Fact]
    public async Task Invoke_AuthenticatedNotComplete_DoesNotCallNext()
    {
        var nextCalled = false;
        var (middleware, client, cache) = CreateSut(_ => { nextCalled = true; return Task.CompletedTask; });
        client.GetOnboardingStatusAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(false);
        var ctx = AuthenticatedContext("/api/v1/ordering/orders");

        await middleware.InvokeAsync(ctx, client, cache);

        nextCalled.Should().BeFalse();
    }

    // ── Onboarding gate: complete ─────────────────────────────────────────────

    [Fact]
    public async Task Invoke_AuthenticatedComplete_PassesThrough()
    {
        var nextCalled = false;
        var (middleware, client, cache) = CreateSut(_ => { nextCalled = true; return Task.CompletedTask; });
        client.GetOnboardingStatusAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);
        var ctx = AuthenticatedContext("/api/v1/catalog/products");

        await middleware.InvokeAsync(ctx, client, cache);

        nextCalled.Should().BeTrue();
        ctx.Response.StatusCode.Should().NotBe(StatusCodes.Status403Forbidden);
    }

    // ── Cache behaviour ───────────────────────────────────────────────────────

    [Fact]
    public async Task Invoke_SecondRequest_UsesCache()
    {
        var (middleware, client, cache) = CreateSut();
        client.GetOnboardingStatusAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(true);

        var ctx1 = AuthenticatedContext("/api/v1/catalog/products");
        await middleware.InvokeAsync(ctx1, client, cache);

        var ctx2 = AuthenticatedContext("/api/v1/catalog/products");
        await middleware.InvokeAsync(ctx2, client, cache);

        // Status client should only be called once; second request hits the cache.
        await client.Received(1).GetOnboardingStatusAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    // ── Cache invalidation after complete-onboarding ──────────────────────────

    [Fact]
    public async Task Invoke_CompleteOnboardingSuccess_EvictsCache()
    {
        // Seed the cache with false so we can verify it gets evicted.
        var (middleware, client, cache) = CreateSut(ctx =>
        {
            ctx.Response.StatusCode = StatusCodes.Status200OK;
            return Task.CompletedTask;
        });
        cache.Set($"onboarding:{TestSub}", false);

        var ctx = AuthenticatedContext("/api/v1/profile/complete-onboarding", "POST");
        await middleware.InvokeAsync(ctx, client, cache);

        // Cache entry should have been evicted so next gated request re-checks.
        cache.TryGetValue($"onboarding:{TestSub}", out bool _).Should().BeFalse();
    }
}
