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
}
