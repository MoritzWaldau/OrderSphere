namespace OrderSphere.BuildingBlocks.StronglyTypedIds;

public readonly record struct ReservationId(Guid Value)
{
    public static ReservationId New() => new(Guid.CreateVersion7());
    public static ReservationId Empty => new(Guid.Empty);
    public static ReservationId From(Guid v) => new(v);

    public override string ToString() => Value.ToString();
}
