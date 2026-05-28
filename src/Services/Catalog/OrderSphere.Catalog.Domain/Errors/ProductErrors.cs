using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Catalog.Domain.Errors;

public static class ProductErrors
{
    public static readonly Error NameAlreadyExists = new("Product.NameAlreadyExists", "A product with this name already exists.", ErrorType.Conflict);
    public static readonly Error InvalidPrice = new("Product.InvalidPrice", "Product price must be greater than zero.", ErrorType.Failure);
    public static readonly Error NotFound = new("Product.NotFound", "Product was not found.", ErrorType.NotFound);
    public static readonly Error InsufficientStock = new("Product.InsufficientStock", "Insufficient stock for the requested quantity.", ErrorType.Conflict);
    public static readonly Error SkuAlreadyExists = new("Product.SkuAlreadyExists", "A product with this SKU already exists.", ErrorType.Conflict);
    public static readonly Error UnknownError = new("Product.UnknownError", "An unexpected error occurred.", ErrorType.Unexpected);
}
