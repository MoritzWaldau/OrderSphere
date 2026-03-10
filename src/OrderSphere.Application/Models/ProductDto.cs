
namespace OrderSphere.Application.Models;

public sealed class ProductDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal Price { get;  init; }
    public int Stock { get;  init; }
}
