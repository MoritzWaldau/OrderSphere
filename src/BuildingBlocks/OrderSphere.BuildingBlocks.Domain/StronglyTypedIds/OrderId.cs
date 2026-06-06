namespace OrderSphere.BuildingBlocks.StronglyTypedIds;

public readonly record struct OrderId(Guid Value)
{
    public static OrderId New() => new(Guid.CreateVersion7());
    public static OrderId Empty => new(Guid.Empty);
    public static OrderId From(Guid v) => new(v);

    public override string ToString() => Value.ToString();
}
