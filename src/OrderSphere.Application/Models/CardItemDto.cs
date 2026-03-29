namespace OrderSphere.Application.Models;

public sealed record CardItemDto(Guid ProductId, string ProductName, decimal Price, int Quantity);
