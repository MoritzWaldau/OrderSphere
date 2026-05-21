using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Ordering.Domain.Errors;

public static class OrderErrors
{
    public static readonly Error UnknownError =
        new("Order.Unknown", "An unknown error occurred.");

    public static readonly Error OrderNotFoundError =
        new("Not found", "Order was not found.");

    public static readonly Error InvalidStatusTransition =
        new("Order.InvalidStatusTransition", "The order's current status does not allow this transition.");
}
