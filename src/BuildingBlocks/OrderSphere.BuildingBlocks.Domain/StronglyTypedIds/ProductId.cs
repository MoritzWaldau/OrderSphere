namespace OrderSphere.BuildingBlocks.StronglyTypedIds;

public readonly record struct ProductId(Guid Value)
{
    public static ProductId New()        => new(Guid.CreateVersion7());
    public static ProductId Empty        => new(Guid.Empty);
    public static ProductId From(Guid v) => new(v);

    public override string ToString() => Value.ToString();
}
