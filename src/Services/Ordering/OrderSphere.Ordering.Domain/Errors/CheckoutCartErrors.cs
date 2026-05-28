using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Ordering.Domain.Errors;

public static class CheckoutCartErrors
{
    public static readonly Error UnknownError =
        new("Checkout.Unknown", "An unknown error occurred.", ErrorType.Unexpected);

    public static readonly Error CartNotFoundError =
        new("Checkout.CartNotFound", "Cart was not found.", ErrorType.NotFound);

    public static readonly Error EmptyCartError =
        new("Checkout.EmptyCart", "Der Warenkorb ist leer.", ErrorType.Failure);
}
