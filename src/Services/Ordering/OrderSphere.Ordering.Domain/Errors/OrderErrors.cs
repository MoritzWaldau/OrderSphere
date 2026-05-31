using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Ordering.Domain.Errors;

public static class OrderErrors
{
    public static readonly Error UnknownError =
        new("Order.Unknown", "An unknown error occurred.", ErrorType.Unexpected);

    public static readonly Error OrderNotFoundError =
        new("Order.NotFound", "Order was not found.", ErrorType.NotFound);

    public static readonly Error InvalidStatusTransition =
        new("Order.InvalidStatusTransition", "The order's current status does not allow this transition.", ErrorType.Conflict);
}
