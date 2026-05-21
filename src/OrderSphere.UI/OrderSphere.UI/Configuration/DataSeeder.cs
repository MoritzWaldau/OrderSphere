using Microsoft.EntityFrameworkCore;
using OrderSphere.Infrastructure.Persistence;

namespace OrderSphere.UI.Configuration;

public static class DataSeeder
{
    public static async Task SeedDataAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderSphereDbContext>();

        await dbContext.Database.MigrateAsync();

        // Product and category seeding is handled by OrderSphere.Catalog.Api.
        // Seed data for the ordering context (carts, orders) does not exist at startup.
    }
}
