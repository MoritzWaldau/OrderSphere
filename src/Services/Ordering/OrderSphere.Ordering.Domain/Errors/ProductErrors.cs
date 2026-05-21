using OrderSphere.Domain.Primitives;

namespace OrderSphere.Ordering.Domain.Errors;

public static class ProductErrors
{
    public static readonly Error ProductNotFoundError =
        new("Not found", "Product was not found.");

    public static readonly Error InsufficientStockError =
        new("Product.InsufficientStock", "Insufficient stock for the requested quantity.");
}
