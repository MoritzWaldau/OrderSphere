using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrderSphere.BuildingBlocks.EventBus.AzureServiceBus.Dlq;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.EventBus.AzureServiceBus.Tests.Dlq.TestSupport;
using Xunit;

namespace OrderSphere.EventBus.AzureServiceBus.Tests.Dlq;

/// <summary>
/// Boots a minimal <see cref="WebApplication"/> on the in-memory <see cref="TestServer"/> with
/// <see cref="DlqEndpointExtensions.MapDlqAdminEndpoints"/> mapped and a <see cref="StubDlqAdmin"/>
/// in place of a real Service Bus admin, to exercise routing, model binding, and the
/// Result-to-HttpResult mapping for every branch.
/// </summary>
public sealed class DlqEndpointExtensionsTests : IAsyncDisposable
{
    private const string RoutePrefix = "api/v1/admin/test/dlq";

    private WebApplication? _app;

    private async Task<HttpClient> StartAppAsync(IDlqAdmin admin)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Logging.ClearProviders();

        builder.Services.AddSingleton(admin);
        builder.Services
            .AddAuthentication(AlwaysAdminAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, AlwaysAdminAuthHandler>(AlwaysAdminAuthHandler.SchemeName, _ => { });
        builder.Services.AddAuthorizationBuilder()
            .AddPolicy("AdminPolicy", policy => policy.RequireRole("admin"));

        _app = builder.Build();
        _app.UseAuthentication();
        _app.UseAuthorization();
        _app.MapDlqAdminEndpoints(RoutePrefix, "AdminPolicy");

        await _app.StartAsync();
        return _app.GetTestClient();
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
            await _app.DisposeAsync();
    }

    [Fact]
    public async Task ListEndpoint_OnSuccess_ReturnsOkWithTheOwnedQueueDepths()
    {
        var admin = new StubDlqAdmin
        {
            OnGetDepths = _ => Task.FromResult(Result<IReadOnlyList<DlqQueueDepth>>.Success(
                [new DlqQueueDepth("orders", 2, Capped: false)]))
        };
        var client = await StartAppAsync(admin);

        var response = await client.GetAsync($"/{RoutePrefix}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var depths = await response.Content.ReadFromJsonAsync<List<DlqQueueDepth>>();
        depths.Should().ContainSingle(d => d.Queue == "orders" && d.Depth == 2);
    }

    [Fact]
    public async Task PeekEndpoint_PassesTheMaxQueryParameterThrough()
    {
        int? capturedMax = null;
        var admin = new StubDlqAdmin
        {
            OnPeek = (_, max, _) =>
            {
                capturedMax = max;
                return Task.FromResult(Result<IReadOnlyList<DeadLetterMessage>>.Success([]));
            }
        };
        var client = await StartAppAsync(admin);

        var response = await client.GetAsync($"/{RoutePrefix}/orders/messages?max=5");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        capturedMax.Should().Be(5);
    }

    [Fact]
    public async Task PeekEndpoint_WithoutAMaxQueryParameter_DefaultsToTwenty()
    {
        int? capturedMax = null;
        var admin = new StubDlqAdmin
        {
            OnPeek = (_, max, _) =>
            {
                capturedMax = max;
                return Task.FromResult(Result<IReadOnlyList<DeadLetterMessage>>.Success([]));
            }
        };
        var client = await StartAppAsync(admin);

        await client.GetAsync($"/{RoutePrefix}/orders/messages");

        capturedMax.Should().Be(20);
    }

    [Fact]
    public async Task ReplayEndpoint_OnSuccess_ReturnsOkWithTheReplayReport()
    {
        var admin = new StubDlqAdmin
        {
            OnReplay = (queue, _, _) => Task.FromResult(Result<DlqReplayReport>.Success(new DlqReplayReport(queue, 3)))
        };
        var client = await StartAppAsync(admin);

        var response = await client.PostAsJsonAsync($"/{RoutePrefix}/orders/replay", new { max = 10 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var report = await response.Content.ReadFromJsonAsync<DlqReplayReport>();
        report!.Replayed.Should().Be(3);
    }

    [Fact]
    public async Task ReplayEndpoint_WithoutABody_DefaultsMaxToTwenty()
    {
        int? capturedMax = null;
        var admin = new StubDlqAdmin
        {
            OnReplay = (_, max, _) =>
            {
                capturedMax = max;
                return Task.FromResult(Result<DlqReplayReport>.Success(new DlqReplayReport("orders", 0)));
            }
        };
        var client = await StartAppAsync(admin);

        await client.PostAsync($"/{RoutePrefix}/orders/replay", content: null);

        capturedMax.Should().Be(20);
    }

    [Theory]
    [InlineData(ErrorType.NotFound, HttpStatusCode.NotFound)]
    [InlineData(ErrorType.Forbidden, HttpStatusCode.Forbidden)]
    [InlineData(ErrorType.Unauthorized, HttpStatusCode.Unauthorized)]
    [InlineData(ErrorType.Failure, HttpStatusCode.BadRequest)]
    [InlineData(ErrorType.Validation, HttpStatusCode.BadRequest)]
    [InlineData(ErrorType.Conflict, HttpStatusCode.BadRequest)]
    [InlineData(ErrorType.Unexpected, HttpStatusCode.BadRequest)]
    public async Task ListEndpoint_OnFailure_MapsEveryErrorTypeToTheExpectedStatusCode(
        ErrorType errorType, HttpStatusCode expected)
    {
        var admin = new StubDlqAdmin
        {
            OnGetDepths = _ => Task.FromResult(Result<IReadOnlyList<DlqQueueDepth>>.Failure(
                new Error("Dlq.Test", "boom", errorType)))
        };
        var client = await StartAppAsync(admin);

        var response = await client.GetAsync($"/{RoutePrefix}");

        response.StatusCode.Should().Be(expected);
    }
}
