namespace OrderSphere.BuildingBlocks.StronglyTypedIds;

public readonly record struct SavedAddressId(Guid Value)
{
    public static SavedAddressId New()        => new(Guid.CreateVersion7());
    public static SavedAddressId Empty        => new(Guid.Empty);
    public static SavedAddressId From(Guid v) => new(v);

    public override string ToString() => Value.ToString();
}
