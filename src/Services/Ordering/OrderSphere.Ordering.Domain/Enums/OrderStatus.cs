using System.Text.Json.Serialization;

namespace OrderSphere.Ordering.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum OrderStatus
{
    Created,
    Paid,
    Shipped,
    Delivered,
    Cancelled
}
