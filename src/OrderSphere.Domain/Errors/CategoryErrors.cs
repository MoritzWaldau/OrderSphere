using OrderSphere.Domain.Primitives;

namespace OrderSphere.Domain.Errors;

public static class CategoryErrors
{
    public static readonly Error UnknownError =
        new("Category.Unknown", "An unknown error occurred.");

    public static readonly Error NotFound =
        new("Category.NotFound", "Category was not found.");

    public static readonly Error HasProducts =
        new("Category.HasProducts", "Category cannot be deleted while it still contains products.");
}
