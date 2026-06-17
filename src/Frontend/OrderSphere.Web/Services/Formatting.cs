using System.Globalization;

namespace OrderSphere.Web.Services;

/// <summary>
/// Central culture-aware formatting. Replaces the scattered <c>ToString("C")</c> and
/// <c>"dd.MM.yyyy HH:mm"</c> literals so number grouping and date layout follow the active UI
/// culture. Prices stay in EUR regardless of language — only the number layout localizes.
/// </summary>
public static class Formatting
{
    /// <summary>Formats a monetary amount in EUR using the current culture's number layout.</summary>
    public static string Currency(decimal value)
    {
        var format = (NumberFormatInfo)CultureInfo.CurrentCulture.NumberFormat.Clone();
        format.CurrencySymbol = "€";
        return value.ToString("C", format);
    }

    /// <summary>Short date + time in the current culture (replaces "dd.MM.yyyy HH:mm").</summary>
    public static string DateTime(DateTime value) => value.ToString("g", CultureInfo.CurrentCulture);

    /// <summary>Short date only in the current culture.</summary>
    public static string Date(DateTime value) => value.ToString("d", CultureInfo.CurrentCulture);
}
