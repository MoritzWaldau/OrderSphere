namespace OrderSphere.BuildingBlocks.StronglyTypedIds;

public readonly record struct CategoryId(Guid Value)
{
    public static CategoryId New()        => new(Guid.CreateVersion7());
    public static CategoryId Empty        => new(Guid.Empty);
    public static CategoryId From(Guid v) => new(v);

    public override string ToString() => Value.ToString();
}
