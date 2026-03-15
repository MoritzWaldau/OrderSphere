namespace OrderSphere.Application.Models;

public sealed record OrderItemDto(Guid ProdicutId, int Quantity, decimal Price);
