using FluentAssertions;
using OrderSphere.BuildingBlocks.ValueObjects;
using Xunit;

namespace OrderSphere.Domain.Tests.ValueObjects;

public sealed class QuantityTests
{
    [Fact]
    public void Of_ValidValue_CreatesInstance()
    {
        var q = Quantity.Of(5);

        q.Value.Should().Be(5);
    }

    [Fact]
    public void Of_Zero_IsAllowed()
    {
        var q = Quantity.Of(0);

        q.Value.Should().Be(0);
    }

    [Fact]
    public void Constructor_NegativeValue_Throws()
    {
        var act = () => Quantity.Of(-1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Addition_IncreasesValue()
    {
        var q = Quantity.Of(3) + 2;

        q.Value.Should().Be(5);
    }

    [Fact]
    public void Subtraction_DecreasesValue()
    {
        var q = Quantity.Of(5) - 2;

        q.Value.Should().Be(3);
    }

    [Fact]
    public void Subtraction_ResultNegative_Throws()
    {
        var act = () => { var _ = Quantity.Of(1) - 5; };

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void ImplicitConversion_ToInt_ReturnsValue()
    {
        Quantity q = Quantity.Of(7);

        int value = q;

        value.Should().Be(7);
    }

    [Fact]
    public void Zero_StaticField_HasValueZero()
    {
        Quantity.Zero.Value.Should().Be(0);
    }

    [Fact]
    public void Equality_SameValue_Equal()
    {
        var a = Quantity.Of(3);
        var b = Quantity.Of(3);

        a.Should().Be(b);
    }
}
