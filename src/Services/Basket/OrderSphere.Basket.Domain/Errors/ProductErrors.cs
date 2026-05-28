using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Basket.Domain.Errors;

public static class ProductErrors
{
    public static readonly Error ProductNotFoundError =
        new("Product.NotFound", "Product was not found.", ErrorType.NotFound);

    public static readonly Error InsufficientStockError =
        new("Product.InsufficientStock", "Insufficient stock for the requested quantity.", ErrorType.Conflict);
}
