using System.Text.Json.Serialization;

namespace OrderSphere.Ordering.Domain.Enums;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PaymentMethod
{
    Invoice,
    CreditCard,
    PayPal
}
