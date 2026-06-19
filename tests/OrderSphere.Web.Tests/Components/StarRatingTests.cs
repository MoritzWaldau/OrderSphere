using Bunit;
using OrderSphere.Web.Components;

namespace OrderSphere.Web.Tests.Components;

public sealed class StarRatingTests : BunitBase
{
    [Fact]
    public void Renders_FiveStarIcons()
    {
        var cut = Render<StarRating>(p => p.Add(c => c.Value, 4.0));

        cut.FindAll(".mud-icon-root").Should().HaveCount(5);
    }

    [Fact]
    public void ShowCount_True_RendersCountText()
    {
        var cut = Render<StarRating>(p =>
        {
            p.Add(c => c.Value, 4.0);
            p.Add(c => c.Count, 12);
            p.Add(c => c.ShowCount, true);
        });

        cut.Markup.Should().Contain("(12)");
    }

    [Fact]
    public void ShowCount_False_HidesCountText()
    {
        var cut = Render<StarRating>(p =>
        {
            p.Add(c => c.Value, 4.0);
            p.Add(c => c.Count, 12);
            p.Add(c => c.ShowCount, false);
        });

        cut.Markup.Should().NotContain("(12)");
    }

    [Fact]
    public void Count_Zero_HidesCountText_EvenWhenShowCountIsTrue()
    {
        var cut = Render<StarRating>(p =>
        {
            p.Add(c => c.Value, 4.0);
            p.Add(c => c.Count, 0);
            p.Add(c => c.ShowCount, true);
        });

        cut.Markup.Should().NotContain("(0)");
    }
}
