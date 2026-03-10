using OrderSphere.Domain.Primitives;

namespace OrderSphere.Domain.Errors;

public static class CartErrors
{
    public static readonly Error UnknownError =
        new("Cart.Unknown", "An unknown error occurred.");
}
