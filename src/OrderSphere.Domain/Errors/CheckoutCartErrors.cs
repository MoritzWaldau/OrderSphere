using OrderSphere.Domain.Enums;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Domain.Errors;

public static class CheckoutCartErrors
{
    public static readonly Error UnknownError =
        new(ErrorCodes.UNKWON_ERROR, "An unknown error occurred.");

    public static readonly Error EmptyCartError =
        new("Checkout.EmptyCart", "Der Warenkorb ist leer.");
}
