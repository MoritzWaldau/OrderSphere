using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Services.Product;

public interface IProductService
{
    Task<Result<Domain.Entities.Product>> CreateProductAsync(string name, string description, decimal price, int stock)
}
