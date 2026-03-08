using OrderSphere.Domain.Primitives;

namespace OrderSphere.Domain.Errors;

public static class ProductErrors
{
    public static readonly Error NameAlreadyExists =
        new("Product.NameExists", "A product with this name already exists.");

    public static readonly Error InvalidPrice =
        new("Product.InvalidPrice", "Price must be greater than zero.");

    public static readonly Error UnknownError =
        new("Product.Unknown", "An unknown error occurred.");
}
