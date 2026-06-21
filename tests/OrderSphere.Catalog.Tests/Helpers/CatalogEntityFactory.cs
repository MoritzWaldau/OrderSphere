namespace OrderSphere.Catalog.Tests.Helpers;

internal static class CatalogEntityFactory
{
    internal static Category MakeCategory(CategoryId id, string name = "Electronics")
    {
        return new Category(name)
        {
            Id = id
        };
    }
}
