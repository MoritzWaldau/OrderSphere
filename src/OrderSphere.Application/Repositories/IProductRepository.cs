using OrderSphere.Domain.Entities;

namespace OrderSphere.Application.Repositories;

public interface IProductRepository
{
    Task<Product?> GetByNameAsync(string name);
    Task AddAsync(Product product);
}
