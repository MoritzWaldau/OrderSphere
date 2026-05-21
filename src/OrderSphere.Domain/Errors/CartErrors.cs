using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.Domain.Enums;

namespace OrderSphere.Domain.Errors;

public static class CartErrors
{
    public static readonly Error UnknownError =
        new("Cart.Unknown", "An unknown error occurred.");

    public static readonly Error CartNotFoundError =
        new(ErrorCodes.NOT_FOUND, "Cart was not found in database");
}
