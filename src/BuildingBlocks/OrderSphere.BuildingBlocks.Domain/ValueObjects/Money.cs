namespace OrderSphere.BuildingBlocks.ValueObjects;

/// <summary>
/// Value object representing an amount in a specific currency.
/// Immutable; the private parameterless constructor exists solely for EF Core
/// ComplexProperty materialisation via reflection.
/// </summary>
public sealed class Money : IEquatable<Money>
{
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = null!;

    // EF Core ComplexProperty materialisation (sets properties via reflection).
    private Money() { }

    /// <param name="amount">Non-negative monetary amount.</param>
    /// <param name="currency">ISO 4217 three-letter currency code (e.g. "EUR").</param>
    public Money(decimal amount, string currency)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount, nameof(amount));
        ArgumentException.ThrowIfNullOrWhiteSpace(currency, nameof(currency));
        if (currency.Length != 3)
            throw new ArgumentException("Currency must be a 3-letter ISO 4217 code.", nameof(currency));

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }


    /// <summary>Creates a <see cref="Money"/> instance using EUR as the default currency.</summary>
    public static Money Of(decimal amount, string currency = "EUR") => new(amount, currency);

    /// <summary>Returns a zero-amount <see cref="Money"/> in the given currency.</summary>
    public static Money Zero(string currency = "EUR") => new(0m, currency);


    /// <summary>
    /// Allows <see cref="Money"/> to flow transparently into <see cref="decimal"/>
    /// parameters (e.g. DTO assignments, arithmetic).
    /// </summary>
    public static implicit operator decimal(Money money) => money.Amount;


    public bool Equals(Money? other) =>
        other is not null && Amount == other.Amount && Currency == other.Currency;

    public override bool Equals(object? obj) => obj is Money m && Equals(m);
    public override int GetHashCode() => HashCode.Combine(Amount, Currency);

    public static bool operator ==(Money? left, Money? right) =>
        left is null ? right is null : left.Equals(right);

    public static bool operator !=(Money? left, Money? right) => !(left == right);

    public override string ToString() =>
        string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:F2} {1}", Amount, Currency);
}

