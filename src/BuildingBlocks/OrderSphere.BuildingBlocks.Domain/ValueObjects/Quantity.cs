namespace OrderSphere.BuildingBlocks.ValueObjects;

/// <summary>
/// Value object representing a non-negative integer quantity.
/// A <see cref="ValueConverter{TModel,TProvider}"/> maps this to an <see langword="int"/>
/// column in every DbContext that registers the convention via
/// <c>configurationBuilder.Properties&lt;Quantity&gt;().HaveConversion&lt;QuantityConverter&gt;()</c>.
/// </summary>
public readonly record struct Quantity
{
    public int Value { get; }

    public Quantity(int value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value, nameof(value));
        Value = value;
    }


    public static Quantity Of(int value) => new(value);

    public static readonly Quantity Zero = new(0);


    /// <summary>Adds <paramref name="amount"/> units. Result must be non-negative.</summary>
    public static Quantity operator +(Quantity q, int amount) => new(q.Value + amount);

    /// <summary>Subtracts <paramref name="amount"/> units. Result must be non-negative.</summary>
    public static Quantity operator -(Quantity q, int amount) => new(q.Value - amount);


    /// <summary>
    /// Allows <see cref="Quantity"/> to flow transparently into <see langword="int"/>
    /// parameters (e.g. DTO assignments, comparisons, arithmetic).
    /// </summary>
    public static implicit operator int(Quantity q) => q.Value;

    public override string ToString() => Value.ToString();
}
