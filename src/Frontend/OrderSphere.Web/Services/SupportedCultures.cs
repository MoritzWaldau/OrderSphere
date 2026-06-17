namespace OrderSphere.Web.Services;

/// <summary>A UI language the storefront offers, keyed by its .NET culture code.</summary>
public sealed record SupportedCulture(string Code, string Name);

/// <summary>
/// Registry of the cultures the storefront ships translations for. The neutral resource is
/// German, so <see cref="Default"/> matches the fallback and any unknown stored value resolves to it.
/// </summary>
public static class SupportedCultures
{
    public const string Default = "de-DE";

    /// <summary>localStorage key holding the user's chosen culture code.</summary>
    public const string StorageKey = "os-culture";

    public static readonly IReadOnlyList<SupportedCulture> All =
    [
        new("de-DE", "Deutsch"),
        new("en-US", "English"),
    ];

    public static bool IsSupported(string? code) => code is not null && All.Any(c => c.Code == code);

    public static string Normalize(string? code) => IsSupported(code) ? code! : Default;
}
