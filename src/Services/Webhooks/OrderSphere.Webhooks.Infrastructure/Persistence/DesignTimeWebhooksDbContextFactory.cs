using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OrderSphere.Webhooks.Infrastructure.Persistence;

/// <summary>
/// Used by EF Core tooling (dotnet ef migrations add) at design time.
/// </summary>
public sealed class DesignTimeWebhooksDbContextFactory : IDesignTimeDbContextFactory<WebhooksDbContext>
{
    public WebhooksDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<WebhooksDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=webhooks-db;Username=postgres;Password=postgres");
        return new WebhooksDbContext(optionsBuilder.Options);
    }
}
