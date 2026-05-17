using Microsoft.Extensions.DependencyInjection;
using MudBlazor.Services;

namespace OrderSphere.UI.Tests;

// Shared bUnit TestContext that registers MudBlazor services and stubs JS interop.
// Components that depend on MudBlazor (most of OrderSphere.UI) will fail to render without this.
public abstract class MudBlazorTestContext : TestContext
{
    protected MudBlazorTestContext()
    {
        Services.AddMudServices();
        JSInterop.Mode = JSRuntimeMode.Loose;
    }
}
