using Microsoft.EntityFrameworkCore;
using OrderSphere.Basket.Domain.Entities;

namespace OrderSphere.Basket.Application.Abstractions;

public interface IBasketDbContext
{
    DbSet<Cart> Carts { get; }
    DbSet<CartItem> CartItems { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
