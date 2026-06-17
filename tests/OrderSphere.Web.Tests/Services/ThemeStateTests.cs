namespace OrderSphere.Web.Tests.Services;

public sealed class ThemeStateTests
{
    [Fact]
    public void DefaultBrand_IsIndigo()
    {
        var sut = new ThemeState();

        sut.CurrentBrand.Id.Should().Be("indigo");
        sut.Theme.Should().NotBeNull();
    }

    [Fact]
    public void AvailableBrands_AreIndigoGreenRed()
    {
        var sut = new ThemeState();

        sut.AvailableBrands.Select(b => b.Id).Should().Equal("indigo", "green", "red");
    }

    [Fact]
    public void SetBrand_Green_SwitchesBrandAndRebuildsTheme()
    {
        var sut = new ThemeState();
        var before = sut.Theme;

        sut.SetBrand("green");

        sut.CurrentBrand.Id.Should().Be("green");
        sut.CurrentBrand.Primary.Should().Be("#1F9D57");
        sut.Theme.Should().NotBeSameAs(before, "the MudTheme is rebuilt for the new brand");
    }

    [Fact]
    public void SetBrand_RaisesOnChange()
    {
        var sut = new ThemeState();
        var raised = false;
        sut.OnChange += () => raised = true;

        sut.SetBrand("red");

        raised.Should().BeTrue();
    }

    [Fact]
    public void SetBrand_UnknownId_IsNoOp()
    {
        var sut = new ThemeState();
        var before = sut.Theme;
        var raised = false;
        sut.OnChange += () => raised = true;

        sut.SetBrand("purple");

        sut.CurrentBrand.Id.Should().Be("indigo");
        sut.Theme.Should().BeSameAs(before);
        raised.Should().BeFalse();
    }

    [Fact]
    public void SetBrand_SameBrand_DoesNotRaiseOnChange()
    {
        var sut = new ThemeState();
        var raised = false;
        sut.OnChange += () => raised = true;

        sut.SetBrand("indigo");

        raised.Should().BeFalse();
    }

    [Fact]
    public void Toggle_FlipsDarkMode_AndRaisesOnChange()
    {
        var sut = new ThemeState();
        var raised = false;
        sut.OnChange += () => raised = true;

        sut.Toggle();

        sut.IsDarkMode.Should().BeTrue();
        raised.Should().BeTrue();
    }
}
