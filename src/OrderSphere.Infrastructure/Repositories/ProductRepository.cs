using Microsoft.EntityFrameworkCore;
using OrderSphere.Application.Repositories;
using OrderSphere.Domain.Entities;
using OrderSphere.Infrastructure.Persistence;

namespace OrderSphere.Infrastructure.Repositories;

public sealed class ProductRepository(OrderSphereDbContext Context) : IProductRepository
{
    public async Task AddAsync(Product product)
    {
        await Context.Products.AddAsync(product);
    }

    public async Task<Product?> GetByNameAsync(string name)
    {
        return await Context.Products.FirstOrDefaultAsync(p => p.Name == name);
    }
}
