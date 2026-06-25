namespace OrderSphere.BuildingBlocks.ExchangeRates;

/// <summary>
/// Supplies currency conversion rates relative to the platform base currency (EUR).
/// Conversion is presentation-only: amounts are stored and computed in the base currency,
/// and converted exclusively for display. Implementations are expected to be cheap and
/// side-effect free so they can be called per request.
/// </summary>
public interface IExchangeRateProvider
{
    /// <summary>The platform base currency that all stored amounts are denominated in.</summary>
    string BaseCurrency { get; }

    /// <summary>
    /// Returns the rate to convert one unit of the base currency into <paramref name="targetCurrency"/>.
    /// Returns 1 when <paramref name="targetCurrency"/> equals the base currency. Throws when the
    /// currency is unknown — an unconfigured currency is a configuration error, not a runtime
    /// business condition.
    /// </summary>
    decimal GetRate(string targetCurrency);

    /// <summary>
    /// Returns every supported currency mapped to its rate against the base currency
    /// (the base currency itself maps to 1). Intended for clients that fetch the full table
    /// once and convert locally for display.
    /// </summary>
    IReadOnlyDictionary<string, decimal> GetRates();
}
