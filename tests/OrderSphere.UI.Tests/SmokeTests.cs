namespace OrderSphere.UI.Tests;

// Sanity check that the bUnit fixture wires MudBlazor services correctly.
// Replace with real component renders (Cart page, Product detail, Checkout) once those
// components' DI dependencies are stubbed via Services.AddSingleton / Substitute.For<T>.
public sealed class SmokeTests : MudBlazorTestContext
{
    [Fact]
    public void TestContext_RendersTrivialMarkup()
    {
        var cut = RenderComponent<TrivialComponent>();

        cut.Markup.Should().Contain("hello");
    }
}

internal sealed class TrivialComponent : Microsoft.AspNetCore.Components.ComponentBase
{
    protected override void BuildRenderTree(Microsoft.AspNetCore.Components.Rendering.RenderTreeBuilder builder)
    {
        builder.AddContent(0, "hello");
    }
}
