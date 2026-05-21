using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Catalog.Domain.Errors;

public static class CategoryErrors
{
    public static readonly Error NotFound = new("Category.NotFound", "Category was not found.");
    public static readonly Error HasProducts = new("Category.HasProducts", "Cannot delete a category that has products assigned to it.");
    public static readonly Error UnknownError = new("Category.UnknownError", "An unexpected error occurred.");
}
