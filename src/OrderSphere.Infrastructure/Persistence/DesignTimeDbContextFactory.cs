using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OrderSphere.Infrastructure.Persistence;

/// <summary>
/// Used by `dotnet ef` at design time. Avoids bootstrapping the UI host (which now
/// requires Keycloak configuration) just to scaffold migrations.
/// </summary>
internal sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<OrderSphereDbContext>
{
    public OrderSphereDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<OrderSphereDbContext>()
            .UseNpgsql("Host=localhost;Database=ordersphere-design;Username=postgres;Password=postgres")
            .Options;

        return new OrderSphereDbContext(options);
    }
}
