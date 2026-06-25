using System.Globalization;

namespace OrderSphere.Web.Services;

/// <summary>
/// Central culture- and currency-aware formatting. Replaces the scattered <c>ToString("C")</c>
/// and <c>"dd.MM.yyyy HH:mm"</c> literals so number grouping and date layout follow the active UI
/// culture.
/// <para>
/// Amounts are passed in the platform base currency (EUR). Display currency and the
/// base→display rate are ambient process state — set once at startup and on a currency switch —
/// mirroring how <see cref="CultureInfo.DefaultThreadCurrentCulture"/> is applied. Blazor WebAssembly
/// is single-user per process, so static ambient state is safe here. Callers keep calling
/// <see cref="Currency(decimal)"/> unchanged; conversion and symbol selection happen centrally.
/// </para>
/// </summary>
public static class Formatting
{
    private static string _displayCurrency = SupportedCurrencies.Default;
    private static string _symbol = "€";
    private static decimal _rate = 1m;

    /// <summary>The currency amounts are currently rendered in (ISO 4217 code).</summary>
    public static string DisplayCurrency => _displayCurrency;

    /// <summary>
    /// Sets the ambient display currency and the conversion rate (units of the display currency
    /// per one unit of the base currency). Called at startup and whenever the user switches currency.
    /// </summary>
    public static void SetDisplayCurrency(string currencyCode, decimal baseToDisplayRate)
    {
        var currency = SupportedCurrencies.Get(currencyCode);
        _displayCurrency = currency.Code;
        _symbol = currency.Symbol;
        _rate = baseToDisplayRate <= 0 ? 1m : baseToDisplayRate;
    }

    /// <summary>
    /// Converts a base-currency (EUR) amount into the active display currency and formats it using
    /// the current culture's number layout. When the display currency is the base currency the
    /// rate is 1 and no conversion occurs.
    /// </summary>
    public static string Currency(decimal baseAmount)
    {
        var converted = Math.Round(baseAmount * _rate, 2, MidpointRounding.AwayFromZero);
        var format = (NumberFormatInfo)CultureInfo.CurrentCulture.NumberFormat.Clone();
        format.CurrencySymbol = _symbol;
        return converted.ToString("C", format);
    }

    /// <summary>Short date + time in the current culture (replaces "dd.MM.yyyy HH:mm").</summary>
    public static string DateTime(DateTime value) => value.ToString("g", CultureInfo.CurrentCulture);

    /// <summary>Short date only in the current culture.</summary>
    public static string Date(DateTime value) => value.ToString("d", CultureInfo.CurrentCulture);
}
