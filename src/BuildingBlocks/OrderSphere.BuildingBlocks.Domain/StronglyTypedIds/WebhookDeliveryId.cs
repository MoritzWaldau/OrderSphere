namespace OrderSphere.BuildingBlocks.StronglyTypedIds;

public readonly record struct WebhookDeliveryId(Guid Value)
{
    public static WebhookDeliveryId New() => new(Guid.CreateVersion7());
    public static WebhookDeliveryId Empty => new(Guid.Empty);
    public static WebhookDeliveryId From(Guid v) => new(v);

    public override string ToString() => Value.ToString();
}
