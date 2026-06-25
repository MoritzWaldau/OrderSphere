namespace OrderSphere.Web.Services;

/// <summary>A display currency the storefront offers, keyed by its ISO 4217 code.</summary>
public sealed record SupportedCurrency(string Code, string Symbol, string Name);

/// <summary>
/// Registry of the currencies the storefront can render. Currency selection is presentation-only:
/// amounts are stored and computed in the base currency (EUR) and converted for display via the
/// rate table fetched from the BFF. <see cref="Default"/> is the base currency and the fallback
/// for any unknown stored value.
/// </summary>
public static class SupportedCurrencies
{
    public const string Default = "EUR";

    /// <summary>localStorage key holding the user's chosen display currency code.</summary>
    public const string StorageKey = "os-currency";

    public static readonly IReadOnlyList<SupportedCurrency> All =
    [
        new("EUR", "€", "Euro"),
        new("USD", "$", "US Dollar"),
        new("GBP", "£", "British Pound"),
        new("CHF", "CHF", "Swiss Franc"),
    ];

    public static bool IsSupported(string? code) =>
        code is not null && All.Any(c => c.Code.Equals(code, StringComparison.OrdinalIgnoreCase));

    public static string Normalize(string? code) =>
        IsSupported(code) ? code!.ToUpperInvariant() : Default;

    public static SupportedCurrency Get(string? code) =>
        All.FirstOrDefault(c => c.Code.Equals(Normalize(code), StringComparison.OrdinalIgnoreCase))
        ?? All[0];
}
