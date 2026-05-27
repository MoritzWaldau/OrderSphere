namespace OrderSphere.BuildingBlocks.StronglyTypedIds;

public readonly record struct PaymentId(Guid Value)
{
    public static PaymentId New()        => new(Guid.CreateVersion7());
    public static PaymentId Empty        => new(Guid.Empty);
    public static PaymentId From(Guid v) => new(v);

    public override string ToString() => Value.ToString();
}
