using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Ordering.Domain.Errors;

public static class CheckoutCartErrors
{
    public static readonly Error UnknownError =
        new("Checkout.Unknown", "An unknown error occurred.");

    public static readonly Error EmptyCartError =
        new("Checkout.EmptyCart", "Der Warenkorb ist leer.");
}
