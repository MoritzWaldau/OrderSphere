using System.Globalization;

namespace OrderSphere.Web.Tests.Services;

/// <summary>
/// Covers the currency-conversion path of <see cref="Formatting"/>. The display currency and rate
/// are ambient process state, so each test restores the base-currency default (EUR, rate 1) to
/// avoid leaking into other tests.
/// </summary>
public sealed class FormattingTests : IDisposable
{
    private readonly CultureInfo _originalCulture = CultureInfo.CurrentCulture;

    public FormattingTests()
        // Deterministic number layout regardless of the test host's locale.
        => CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

    [Fact]
    public void Currency_BaseCurrency_NoConversion()
    {
        Formatting.SetDisplayCurrency("EUR", 1m);

        var text = Formatting.Currency(10m);

        text.Should().Contain("10").And.Contain("€");
    }

    [Fact]
    public void Currency_ForeignCurrency_ConvertsAndUsesSymbol()
    {
        Formatting.SetDisplayCurrency("USD", 1.1m);

        var text = Formatting.Currency(10m);

        // 10 EUR * 1.1 = 11.00 USD
        text.Should().Contain("11").And.Contain("$");
        Formatting.DisplayCurrency.Should().Be("USD");
    }

    [Fact]
    public void Currency_NonPositiveRate_FallsBackToRateOne()
    {
        Formatting.SetDisplayCurrency("USD", 0m);

        var text = Formatting.Currency(10m);

        text.Should().Contain("10");
    }

    public void Dispose()
    {
        Formatting.SetDisplayCurrency("EUR", 1m);
        CultureInfo.CurrentCulture = _originalCulture;
    }
}
