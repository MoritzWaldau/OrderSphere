namespace OrderSphere.Catalog.Application.Caching;

internal static class CatalogCache
{
    public const string Tag = "catalog";
    public static string ProductBySlugKey(string slug) => $"catalog:product:slug:{slug}";
}
