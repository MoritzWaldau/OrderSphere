using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using OrderSphere.BuildingBlocks.ExchangeRates;

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Configuration for the static exchange-rate table, bound from the "ExchangeRates" section.
/// Rates are expressed as units of the target currency per one unit of <see cref="BaseCurrency"/>.
/// The base currency is implicit (rate 1) and need not appear in <see cref="Rates"/>.
/// </summary>
public sealed class ExchangeRateOptions
{
    public const string SectionName = "ExchangeRates";

    public string BaseCurrency { get; set; } = "EUR";

    public Dictionary<string, decimal> Rates { get; set; } = new();
}

/// <summary>
/// <see cref="IExchangeRateProvider"/> backed by a configured static rate table. Deterministic
/// and dependency-free so it is trivially testable; the interface stays swappable for a future
/// live-rate provider without touching consumers.
/// </summary>
internal sealed class ConfiguredExchangeRateProvider : IExchangeRateProvider
{
    private readonly string _baseCurrency;
    private readonly IReadOnlyDictionary<string, decimal> _rates;

    public ConfiguredExchangeRateProvider(IOptions<ExchangeRateOptions> options)
    {
        var value = options.Value;
        _baseCurrency = Normalize(value.BaseCurrency);

        // Materialise once: base currency maps to 1, plus every configured target rate.
        var rates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase)
        {
            [_baseCurrency] = 1m,
        };
        foreach (var (currency, rate) in value.Rates)
            rates[Normalize(currency)] = rate;

        _rates = rates;
    }

    public string BaseCurrency => _baseCurrency;

    public decimal GetRate(string targetCurrency)
    {
        var currency = Normalize(targetCurrency);
        if (_rates.TryGetValue(currency, out var rate))
            return rate;

        throw new KeyNotFoundException(
            $"No exchange rate configured for currency '{currency}' (base {_baseCurrency}).");
    }

    public IReadOnlyDictionary<string, decimal> GetRates() => _rates;

    private static string Normalize(string currency) => currency.Trim().ToUpperInvariant();
}

/// <summary>
/// Registers the configured exchange-rate provider. Call from a composition root that needs
/// currency conversion (e.g. the gateway that exposes the rate table to the client).
/// </summary>
public static class ExchangeRateExtensions
{
    public static IServiceCollection AddExchangeRates(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<ExchangeRateOptions>(configuration.GetSection(ExchangeRateOptions.SectionName));
        services.AddSingleton<IExchangeRateProvider, ConfiguredExchangeRateProvider>();
        return services;
    }
}
