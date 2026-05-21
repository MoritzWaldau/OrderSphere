using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.Domain.Enums;

namespace OrderSphere.Domain.Errors;

public static class OrderErrors
{
    public static readonly Error UnknownError =
        new("Order.Unknown", "An unknown error occurred.");

    public static readonly Error OrderNotFoundError =
        new(ErrorCodes.NOT_FOUND, "Order was not found in database");

    public static readonly Error InvalidStatusTransition =
        new("Order.InvalidStatusTransition", "The order's current status does not allow this transition.");
}
