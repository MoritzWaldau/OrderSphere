namespace OrderSphere.BuildingBlocks.StronglyTypedIds;

public readonly record struct BrandId(Guid Value)
{
    public static BrandId New() => new(Guid.CreateVersion7());
    public static BrandId Empty => new(Guid.Empty);
    public static BrandId From(Guid v) => new(v);

    public override string ToString() => Value.ToString();
}
