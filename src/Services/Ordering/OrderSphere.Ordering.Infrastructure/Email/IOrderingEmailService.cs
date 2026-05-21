namespace OrderSphere.Ordering.Infrastructure.Email;

public interface IOrderingEmailService
{
    Task SendOrderConfirmationAsync(string toEmail, OrderingConfirmationData data);
}

public sealed record OrderingConfirmationData(
    Guid OrderId,
    string TrackingNumber,
    string ShippingFirstName,
    string ShippingLastName,
    string ShippingStreet,
    string ShippingCity,
    string ShippingPostalCode,
    string ShippingCountry,
    IReadOnlyList<OrderingConfirmationLine> Items,
    decimal Total);

public sealed record OrderingConfirmationLine(string ProductName, int Quantity, decimal Price);
