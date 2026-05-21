using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.Domain.Enums;

namespace OrderSphere.Domain.Errors;

public static class CheckoutCartErrors
{
    public static readonly Error UnknownError =
        new(ErrorCodes.UNKNOWN_ERROR, "An unknown error occurred.");

    public static readonly Error EmptyCartError =
        new("Checkout.EmptyCart", "The cart is empty.");
}
