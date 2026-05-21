using OrderSphere.Domain.Primitives;

namespace OrderSphere.Catalog.Domain.Errors;

public static class ProductErrors
{
    public static readonly Error NameAlreadyExists = new("Product.NameAlreadyExists", "A product with this name already exists.");
    public static readonly Error InvalidPrice = new("Product.InvalidPrice", "Product price must be greater than zero.");
    public static readonly Error NotFound = new("Product.NotFound", "Product was not found.");
    public static readonly Error InsufficientStock = new("Product.InsufficientStock", "Insufficient stock for the requested quantity.");
    public static readonly Error UnknownError = new("Product.UnknownError", "An unexpected error occurred.");
}
