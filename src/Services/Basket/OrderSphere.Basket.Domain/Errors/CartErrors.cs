using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Basket.Domain.Errors;

public static class CartErrors
{
    public static readonly Error UnknownError =
        new("Cart.Unknown", "An unknown error occurred.");

    public static readonly Error CartNotFoundError =
        new("Cart.NotFound", "Cart was not found.");
}
