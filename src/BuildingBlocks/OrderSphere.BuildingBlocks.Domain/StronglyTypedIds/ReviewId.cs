namespace OrderSphere.BuildingBlocks.StronglyTypedIds;

public readonly record struct ReviewId(Guid Value)
{
    public static ReviewId New() => new(Guid.CreateVersion7());
    public static ReviewId Empty => new(Guid.Empty);
    public static ReviewId From(Guid v) => new(v);

    public override string ToString() => Value.ToString();
}
