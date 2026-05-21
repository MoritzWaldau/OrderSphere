using OrderSphere.Domain.Enums;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Domain.Errors;

public static class CheckoutCartErrors
{
    public static readonly Error UnknownError =
        new(ErrorCodes.UNKNOWN_ERROR, "An unknown error occurred.");

    public static readonly Error EmptyCartError =
        new("Checkout.EmptyCart", "The cart is empty.");
}
