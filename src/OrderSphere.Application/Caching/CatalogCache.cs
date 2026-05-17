namespace OrderSphere.Application.Caching;

internal static class CatalogCache
{
    public const string Tag = "catalog";
    public const string ProductsAllKey = "catalog:products:all";
    public const string CategoriesAllKey = "catalog:categories:all";

    public static string ProductBySlugKey(string slug) => $"catalog:product:slug:{slug}";
}
