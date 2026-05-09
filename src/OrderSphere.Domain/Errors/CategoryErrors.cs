using OrderSphere.Domain.Primitives;

namespace OrderSphere.Domain.Errors;

public static class CategoryErrors
{
    public static readonly Error UnknownError =
        new("Category.Unknown", "An unknown error occurred.");
}
