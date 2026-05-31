namespace OrderSphere.BuildingBlocks.StronglyTypedIds;

public readonly record struct CustomerId(Guid Value)
{
    public static CustomerId New()        => new(Guid.CreateVersion7());
    public static CustomerId Empty        => new(Guid.Empty);
    public static CustomerId From(Guid v) => new(v);

    public override string ToString() => Value.ToString();
}
