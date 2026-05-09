using OrderSphere.Domain.Enums;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Domain.Errors;

public static class OrderErrors
{
    public static readonly Error UnknownError =
        new("Order.Unknown", "An unknown error occurred.");

    public static readonly Error OrderNotFoundError =
        new(ErrorCodes.NOT_FOUND, "Order was not found in database");
}
