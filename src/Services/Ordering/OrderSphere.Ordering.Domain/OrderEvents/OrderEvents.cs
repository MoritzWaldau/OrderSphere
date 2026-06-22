namespace OrderSphere.Ordering.Domain.OrderEvents;

/// <summary>The order was placed. Carries the full initial state of the aggregate.</summary>
public sealed record OrderCreated(
    Guid CustomerId,
    OrderAddressData ShippingAddress,
    int PaymentMethod,
    IReadOnlyList<OrderLineData> Items,
    Guid CorrelationId,
    DateTime OccurredAt) : IOrderEvent;

/// <summary>A coupon was redeemed against the order subtotal.</summary>
public sealed record CouponApplied(string CouponCode, decimal DiscountAmount, DateTime OccurredAt) : IOrderEvent;

/// <summary>The shipping cost was calculated and recorded.</summary>
public sealed record ShippingCostSet(decimal Amount, DateTime OccurredAt) : IOrderEvent;

/// <summary>Payment succeeded; the order is confirmed and assigned a tracking number.</summary>
public sealed record OrderConfirmed(string TrackingNumber, DateTime OccurredAt) : IOrderEvent;

/// <summary>The order was handed to the carrier.</summary>
public sealed record OrderShipped(DateTime OccurredAt) : IOrderEvent;

/// <summary>The order was delivered to the customer.</summary>
public sealed record OrderDelivered(DateTime OccurredAt) : IOrderEvent;

/// <summary>The order was cancelled (payment failure, compensation, or admin action).</summary>
public sealed record OrderCancelled(DateTime OccurredAt) : IOrderEvent;

/// <summary>Primitive snapshot of the shipping address carried in <see cref="OrderCreated"/>.</summary>
public sealed record OrderAddressData(
    string FirstName,
    string LastName,
    string Street,
    string City,
    string PostalCode,
    string Country);

/// <summary>Primitive snapshot of an ordered line carried in <see cref="OrderCreated"/>.</summary>
public sealed record OrderLineData(Guid ProductId, string ProductName, int Quantity, decimal Price);
