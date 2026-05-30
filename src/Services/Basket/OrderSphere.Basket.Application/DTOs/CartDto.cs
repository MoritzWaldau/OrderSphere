namespace OrderSphere.Basket.Application.DTOs;

public sealed record CartDto(Guid CustomerId, List<CartItemDto> Items);

public sealed record CartItemDto(Guid ProductId, string ProductName, decimal Price, int Quantity);
