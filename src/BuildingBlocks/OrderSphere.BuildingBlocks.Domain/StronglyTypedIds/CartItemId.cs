namespace OrderSphere.BuildingBlocks.StronglyTypedIds;

public readonly record struct CartItemId(Guid Value)
{
    public static CartItemId New()        => new(Guid.CreateVersion7());
    public static CartItemId Empty        => new(Guid.Empty);
    public static CartItemId From(Guid v) => new(v);

    public override string ToString() => Value.ToString();
}
