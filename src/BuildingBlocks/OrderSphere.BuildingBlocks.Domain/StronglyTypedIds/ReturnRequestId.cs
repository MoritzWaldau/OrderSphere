namespace OrderSphere.BuildingBlocks.StronglyTypedIds;

public readonly record struct ReturnRequestId(Guid Value)
{
    public static ReturnRequestId New() => new(Guid.CreateVersion7());
    public static ReturnRequestId Empty => new(Guid.Empty);
    public static ReturnRequestId From(Guid v) => new(v);

    public override string ToString() => Value.ToString();
}
