namespace OrderSphere.BuildingBlocks.StronglyTypedIds;

public readonly record struct CartId(Guid Value)
{
    public static CartId New()        => new(Guid.CreateVersion7());
    public static CartId Empty        => new(Guid.Empty);
    public static CartId From(Guid v) => new(v);

    public override string ToString() => Value.ToString();
}
