using Microsoft.EntityFrameworkCore;
using OrderSphere.Basket.Infrastructure.Persistence;
using OrderSphere.BuildingBlocks.Abstraction;

namespace OrderSphere.Basket.Tests.Helpers;

/// <summary>
/// Creates an isolated <see cref="BasketDbContext"/> backed by the EF Core in-memory provider.
/// Each call produces a fresh database to prevent cross-test state leakage.
/// </summary>
internal static class BasketDbContextFactory
{
    internal static BasketDbContext Create()
    {
        var options = new DbContextOptionsBuilder<BasketDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new BasketDbContext(options, NullPublisher.Instance);
    }
}
