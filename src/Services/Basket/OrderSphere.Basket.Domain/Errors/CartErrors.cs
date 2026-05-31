using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Basket.Domain.Errors;

public static class CartErrors
{
    public static readonly Error UnknownError =
        new("Cart.Unknown", "An unknown error occurred.", ErrorType.Unexpected);

    public static readonly Error CartNotFoundError =
        new("Cart.NotFound", "Cart was not found.", ErrorType.NotFound);

    public static readonly Error ItemNotFoundError =
        new("Cart.ItemNotFound", "Item was not found in the cart.", ErrorType.NotFound);
}
