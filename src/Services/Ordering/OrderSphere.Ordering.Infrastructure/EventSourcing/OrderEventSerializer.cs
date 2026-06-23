using System.Text.Json;
using OrderSphere.Ordering.Domain.OrderEvents;

namespace OrderSphere.Ordering.Infrastructure.EventSourcing;

/// <summary>
/// Maps order events to and from their stored form. The type name is persisted as an explicit
/// discriminator (not the CLR full name) so the stream stays decoupled from namespace/assembly
/// moves: only this registry needs updating if a CLR type is renamed or relocated.
/// </summary>
internal static class OrderEventSerializer
{
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    private static readonly IReadOnlyDictionary<string, Type> TypesByName = new Dictionary<string, Type>
    {
        [nameof(OrderCreated)] = typeof(OrderCreated),
        [nameof(CouponApplied)] = typeof(CouponApplied),
        [nameof(ShippingCostSet)] = typeof(ShippingCostSet),
        [nameof(OrderConfirmed)] = typeof(OrderConfirmed),
        [nameof(OrderShipped)] = typeof(OrderShipped),
        [nameof(OrderDelivered)] = typeof(OrderDelivered),
        [nameof(OrderCancelled)] = typeof(OrderCancelled),
    };

    public static string TypeName(IOrderEvent @event) => @event.GetType().Name;

    public static string Serialize(IOrderEvent @event) => JsonSerializer.Serialize(@event, @event.GetType(), Options);

    public static IOrderEvent Deserialize(string eventType, string payload)
    {
        if (!TypesByName.TryGetValue(eventType, out var type))
            throw new InvalidOperationException($"Unknown order event type '{eventType}'.");

        return (IOrderEvent)JsonSerializer.Deserialize(payload, type, Options)!;
    }
}
