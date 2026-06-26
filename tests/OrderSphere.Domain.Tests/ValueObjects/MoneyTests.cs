using FluentAssertions;
using OrderSphere.BuildingBlocks.ValueObjects;
using Xunit;

namespace OrderSphere.Domain.Tests.ValueObjects;

public sealed class MoneyTests
{
    [Fact]
    public void Of_ValidAmountAndCurrency_CreatesInstance()
    {
        var money = Money.Of(9.99m, "EUR");

        money.Amount.Should().Be(9.99m);
        money.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Of_DefaultCurrency_UsesEur()
    {
        var money = Money.Of(5m);

        money.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Constructor_NegativeAmount_Throws()
    {
        var act = () => Money.Of(-1m, "EUR");

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Constructor_CurrencyNotThreeChars_Throws()
    {
        var act = () => Money.Of(10m, "EU");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_NullOrWhitespaceCurrency_Throws()
    {
        var act = () => Money.Of(10m, "");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_LowercaseCurrency_NormalisesToUppercase()
    {
        var money = Money.Of(10m, "eur");

        money.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Zero_ReturnsZeroAmount()
    {
        var zero = Money.Zero();

        zero.Amount.Should().Be(0m);
        zero.Currency.Should().Be("EUR");
    }

    [Fact]
    public void Equality_SameAmountAndCurrency_Equal()
    {
        var a = Money.Of(10m, "EUR");
        var b = Money.Of(10m, "EUR");

        a.Should().Be(b);
        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Equality_DifferentCurrency_NotEqual()
    {
        var a = Money.Of(10m, "EUR");
        var b = Money.Of(10m, "USD");

        a.Should().NotBe(b);
        (a != b).Should().BeTrue();
    }

    [Fact]
    public void ImplicitConversion_ToDecimal_ReturnsAmount()
    {
        Money money = Money.Of(42.5m, "EUR");

        decimal value = money;

        value.Should().Be(42.5m);
    }

    [Fact]
    public void ToString_FormatsAmountAndCurrency()
    {
        var money = Money.Of(9.99m, "EUR");

        money.ToString().Should().Be("9.99 EUR");
    }

    [Fact]
    public void Addition_SameCurrency_SumsAmounts()
    {
        var sum = Money.Of(10m, "EUR") + Money.Of(2.50m, "EUR");

        sum.Should().Be(Money.Of(12.50m, "EUR"));
    }

    [Fact]
    public void Subtraction_SameCurrency_SubtractsAmounts()
    {
        var diff = Money.Of(10m, "EUR") - Money.Of(2.50m, "EUR");

        diff.Should().Be(Money.Of(7.50m, "EUR"));
    }

    [Fact]
    public void Addition_DifferentCurrencies_Throws()
    {
        var act = () => _ = Money.Of(10m, "EUR") + Money.Of(2m, "USD");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Subtraction_DifferentCurrencies_Throws()
    {
        var act = () => _ = Money.Of(10m, "EUR") - Money.Of(2m, "USD");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Multiplication_ByIntFactor_ScalesAmount()
    {
        var total = Money.Of(9.99m, "EUR") * 3;

        total.Should().Be(Money.Of(29.97m, "EUR"));
    }

    [Fact]
    public void Multiplication_ByDecimalFactor_ScalesAmount()
    {
        var scaled = Money.Of(10m, "EUR") * 0.5m;

        scaled.Should().Be(Money.Of(5m, "EUR"));
    }

    [Fact]
    public void ConvertTo_AppliesRateAndRoundsToTwoDecimals()
    {
        var converted = Money.Of(10m, "EUR").ConvertTo("USD", 1.085m);

        converted.Amount.Should().Be(10.85m);
        converted.Currency.Should().Be("USD");
    }

    [Fact]
    public void ConvertTo_RoundsAwayFromZeroAtMidpoint()
    {
        // 9.99 * 1.005 = 10.03995 -> rounds to 10.04
        var converted = Money.Of(9.99m, "EUR").ConvertTo("USD", 1.005m);

        converted.Amount.Should().Be(10.04m);
    }

    [Fact]
    public void ConvertTo_SameCurrencyWithRateOne_IsNoOp()
    {
        var converted = Money.Of(42.50m, "EUR").ConvertTo("EUR", 1m);

        converted.Should().Be(Money.Of(42.50m, "EUR"));
    }

    [Fact]
    public void ConvertTo_NonPositiveRate_Throws()
    {
        var act = () => Money.Of(10m, "EUR").ConvertTo("USD", 0m);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
