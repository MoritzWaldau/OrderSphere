namespace OrderSphere.BuildingBlocks.StronglyTypedIds;

public readonly record struct CouponId(Guid Value)
{
    public static CouponId New() => new(Guid.CreateVersion7());
    public static CouponId Empty => new(Guid.Empty);
    public static CouponId From(Guid v) => new(v);

    public override string ToString() => Value.ToString();
}
