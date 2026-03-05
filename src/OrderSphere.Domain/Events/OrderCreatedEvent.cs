namespace OrderSphere.Domain.Events;

public sealed record OrderCreatedEvent(Guid OrderId, Guid CustomerId, DateTime CreatedAt);
