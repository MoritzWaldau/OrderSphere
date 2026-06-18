using OrderSphere.Catalog.Domain.Entities;

namespace OrderSphere.Catalog.Application.Abstractions;

public interface ICatalogDbContext
{
    DbSet<Product> Products { get; }
    DbSet<Category> Categories { get; }
    DbSet<ProductReview> Reviews { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
