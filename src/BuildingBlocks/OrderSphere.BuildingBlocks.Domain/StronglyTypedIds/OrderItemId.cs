namespace OrderSphere.BuildingBlocks.StronglyTypedIds;

public readonly record struct OrderItemId(Guid Value)
{
    public static OrderItemId New()        => new(Guid.CreateVersion7());
    public static OrderItemId Empty        => new(Guid.Empty);
    public static OrderItemId From(Guid v) => new(v);

    public override string ToString() => Value.ToString();
}
