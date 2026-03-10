namespace OrderSphere.Application.Models;

public sealed record CartDto(Guid CustomerId, List<CardItemDto> Items);
