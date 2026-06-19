using Bunit;
using MudBlazor.Services;
using OrderSphere.Web.Components;

namespace OrderSphere.Web.Tests.Components;

public sealed class AppErrorBoundaryTests : TestContext
{
    public AppErrorBoundaryTests()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }

    [Fact]
    public void RendersChildContent_WhenNoError()
    {
        var cut = RenderComponent<AppErrorBoundary>(
            parameters => parameters.AddChildContent("<p>child works</p>"));

        cut.Markup.Should().Contain("child works");
    }

    [Fact]
    public void ShowsFallback_WhenChildThrows()
    {
        var cut = RenderComponent<AppErrorBoundary>(
            parameters => parameters.AddChildContent<ThrowingChild>());

        cut.Markup.Should().Contain("Etwas ist schiefgelaufen");
    }

    private sealed class ThrowingChild : Microsoft.AspNetCore.Components.ComponentBase
    {
        protected override void OnParametersSet() => throw new InvalidOperationException("boom");
    }
}
