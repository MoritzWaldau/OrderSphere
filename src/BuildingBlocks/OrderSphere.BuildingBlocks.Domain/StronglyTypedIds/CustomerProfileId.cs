namespace OrderSphere.BuildingBlocks.StronglyTypedIds;

public readonly record struct CustomerProfileId(Guid Value)
{
    public static CustomerProfileId New() => new(Guid.CreateVersion7());
    public static CustomerProfileId Empty => new(Guid.Empty);
    public static CustomerProfileId From(Guid v) => new(v);

    public override string ToString() => Value.ToString();
}
