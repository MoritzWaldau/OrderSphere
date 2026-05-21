using OrderSphere.Domain.Primitives;

namespace OrderSphere.Ordering.Domain.Errors;

public static class CartErrors
{
    public static readonly Error UnknownError =
        new("Cart.Unknown", "An unknown error occurred.");

    public static readonly Error CartNotFoundError =
        new("Not found", "Cart was not found.");
}
