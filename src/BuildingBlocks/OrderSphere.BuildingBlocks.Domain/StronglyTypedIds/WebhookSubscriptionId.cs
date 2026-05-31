namespace OrderSphere.BuildingBlocks.StronglyTypedIds;

public readonly record struct WebhookSubscriptionId(Guid Value)
{
    public static WebhookSubscriptionId New()        => new(Guid.CreateVersion7());
    public static WebhookSubscriptionId Empty        => new(Guid.Empty);
    public static WebhookSubscriptionId From(Guid v) => new(v);

    public override string ToString() => Value.ToString();
}
