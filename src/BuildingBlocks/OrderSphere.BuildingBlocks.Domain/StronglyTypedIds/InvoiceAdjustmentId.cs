namespace OrderSphere.BuildingBlocks.StronglyTypedIds;

public readonly record struct InvoiceAdjustmentId(Guid Value)
{
    public static InvoiceAdjustmentId New() => new(Guid.CreateVersion7());
    public static InvoiceAdjustmentId Empty => new(Guid.Empty);
    public static InvoiceAdjustmentId From(Guid v) => new(v);

    public override string ToString() => Value.ToString();
}
