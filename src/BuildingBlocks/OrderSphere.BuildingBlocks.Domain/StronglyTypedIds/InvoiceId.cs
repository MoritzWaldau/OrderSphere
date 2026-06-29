namespace OrderSphere.BuildingBlocks.StronglyTypedIds;

public readonly record struct InvoiceId(Guid Value)
{
    public static InvoiceId New() => new(Guid.CreateVersion7());
    public static InvoiceId Empty => new(Guid.Empty);
    public static InvoiceId From(Guid v) => new(v);

    public override string ToString() => Value.ToString();
}
