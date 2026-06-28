using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Ordering.Domain.Errors;

public static class ReturnErrors
{
    public static readonly Error OrderNotFound =
        new("Return.OrderNotFound", "Die Bestellung wurde nicht gefunden.", ErrorType.NotFound);

    public static readonly Error NotOrderOwner =
        new("Return.NotOrderOwner", "Diese Bestellung gehört nicht zum aktuellen Konto.", ErrorType.Failure);

    public static readonly Error OrderNotReturnable =
        new("Return.OrderNotReturnable",
            "Für diese Bestellung kann derzeit keine Rückgabe angelegt werden.", ErrorType.Conflict);

    public static readonly Error NoItems =
        new("Return.NoItems", "Eine Rückgabe muss mindestens eine Position enthalten.", ErrorType.Failure);

    public static readonly Error UnknownItem =
        new("Return.UnknownItem", "Eine angeforderte Position gehört nicht zur Bestellung.", ErrorType.Failure);

    public static readonly Error QuantityExceedsOrdered =
        new("Return.QuantityExceedsOrdered",
            "Die Rückgabemenge übersteigt die bestellte Menge.", ErrorType.Failure);

    public static readonly Error NotFound =
        new("Return.NotFound", "Die Rückgabe wurde nicht gefunden.", ErrorType.NotFound);

    public static readonly Error InvalidStatusTransition =
        new("Return.InvalidStatusTransition",
            "Der aktuelle Status der Rückgabe lässt diesen Übergang nicht zu.", ErrorType.Conflict);
}
