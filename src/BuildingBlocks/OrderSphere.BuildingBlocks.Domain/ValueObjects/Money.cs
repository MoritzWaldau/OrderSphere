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


    /// <summary>Adds two amounts of the same currency.</summary>
    public static Money operator +(Money left, Money right) =>
        new(left.Amount + EnsureSameCurrency(left, right).Amount, left.Currency);

    /// <summary>Subtracts two amounts of the same currency. The result must not go negative.</summary>
    public static Money operator -(Money left, Money right) =>
        new(left.Amount - EnsureSameCurrency(left, right).Amount, left.Currency);

    /// <summary>Scales an amount by an integer factor (e.g. unit price × quantity).</summary>
    public static Money operator *(Money money, int factor) => new(money.Amount * factor, money.Currency);

    /// <summary>Scales an amount by a decimal factor.</summary>
    public static Money operator *(Money money, decimal factor) => new(money.Amount * factor, money.Currency);

    /// <summary>
    /// Converts this amount to <paramref name="targetCurrency"/> using <paramref name="rate"/>
    /// (units of target currency per one unit of this currency). Pure: the caller supplies the
    /// rate, so this method carries no dependency on a rate source. The result is rounded to two
    /// decimal places. Converting to the same currency with rate 1 is a no-op.
    /// </summary>
    public Money ConvertTo(string targetCurrency, decimal rate)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(rate, nameof(rate));
        var converted = Math.Round(Amount * rate, 2, MidpointRounding.AwayFromZero);
        return new Money(converted, targetCurrency);
    }

    /// <summary>
    /// Guards against silently combining different currencies — a programmer error, not a
    /// business condition, so it throws rather than returning a <c>Result</c> failure.
    /// </summary>
    private static Money EnsureSameCurrency(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException(
                $"Cannot operate on amounts of different currencies: {left.Currency} and {right.Currency}.");
        return right;
    }


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
