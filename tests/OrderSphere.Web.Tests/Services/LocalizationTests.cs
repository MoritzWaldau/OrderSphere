using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using OrderSphere.Web;

namespace OrderSphere.Web.Tests.Services;

public sealed class LocalizationTests
{
    private static IStringLocalizer<AppStrings> Localizer()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalization(options => options.ResourcesPath = "Resources");
        return services.BuildServiceProvider().GetRequiredService<IStringLocalizer<AppStrings>>();
    }

    private static T WithCulture<T>(string code, Func<IStringLocalizer<AppStrings>, T> read)
    {
        var previous = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo(code);
            return read(Localizer());
        }
        finally
        {
            CultureInfo.CurrentUICulture = previous;
        }
    }

    [Fact]
    public void NeutralCulture_ResolvesGerman()
    {
        var value = WithCulture("de-DE", l => l["Cart.Title"].Value);

        value.Should().Be("Warenkorb");
    }

    [Fact]
    public void EnglishCulture_ResolvesEnglish()
    {
        var value = WithCulture("en-US", l => l["Cart.Title"].Value);

        value.Should().Be("Cart");
    }

    [Fact]
    public void Resource_IsFound_NotFallenBackToKey()
    {
        var entry = WithCulture("en-US", l => l["Checkout.Submit"]);

        entry.ResourceNotFound.Should().BeFalse();
        entry.Value.Should().Be("Place order");
    }

    [Fact]
    public void FormattedString_SubstitutesArguments()
    {
        var value = WithCulture("en-US", l => l["Cart.AriaLabel", 3].Value);

        value.Should().Be("Cart with 3 items");
    }

    [Fact]
    public void UnknownCulture_FallsBackToNeutralGerman()
    {
        var value = WithCulture("fr-FR", l => l["Cart.Title"].Value);

        value.Should().Be("Warenkorb");
    }

    [Fact]
    public void EnglishResource_CoversEveryNeutralKey()
    {
        var manager = new System.Resources.ResourceManager(
            "OrderSphere.Web.Resources.AppStrings", typeof(AppStrings).Assembly);

        var neutral = KeysOf(manager, CultureInfo.InvariantCulture);
        var english = KeysOf(manager, new CultureInfo("en"));

        // Every German (neutral) key must have an English translation — no silent fallback gaps.
        neutral.Except(english).Should().BeEmpty("every neutral key needs an English entry");
    }

    private static HashSet<string> KeysOf(System.Resources.ResourceManager manager, CultureInfo culture)
    {
        using var set = manager.GetResourceSet(culture, createIfNotExists: true, tryParents: false)!;
        return set.Cast<System.Collections.DictionaryEntry>()
                  .Select(entry => (string)entry.Key)
                  .ToHashSet();
    }
}
