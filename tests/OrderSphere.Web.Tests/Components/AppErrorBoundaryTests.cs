using Bunit;
using MudBlazor.Services;
using OrderSphere.Web.Components;

namespace OrderSphere.Web.Tests.Components;

public sealed class AppErrorBoundaryTests : BunitContext, IAsyncLifetime
{
    public AppErrorBoundaryTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    Task IAsyncLifetime.InitializeAsync() => Task.CompletedTask;

    async Task IAsyncLifetime.DisposeAsync() => await DisposeAsync();

    [Fact]
    public void RendersChildContent_WhenNoError()
    {
        var cut = Render<AppErrorBoundary>(
            parameters => parameters.AddChildContent("<p>child works</p>"));

        cut.Markup.Should().Contain("child works");
    }

    [Fact]
    public void ShowsFallback_WhenChildThrows()
    {
        var cut = Render<AppErrorBoundary>(
            parameters => parameters.AddChildContent<ThrowingChild>());

        cut.Markup.Should().Contain("Etwas ist schiefgelaufen");
    }

    private sealed class ThrowingChild : Microsoft.AspNetCore.Components.ComponentBase
    {
        protected override void OnParametersSet() => throw new InvalidOperationException("boom");
    }
}
