using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace OrderSphere.Notification.Worker.Persistence;

/// <summary>
/// Design-time factory used by EF Core tooling (dotnet ef migrations add …).
/// </summary>
internal sealed class NotificationDbContextFactory : IDesignTimeDbContextFactory<NotificationDbContext>
{
    public NotificationDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<NotificationDbContext>()
            .UseNpgsql("Host=localhost;Database=notification-db;Username=postgres;Password=postgres")
            .Options;

        return new NotificationDbContext(options);
    }
}
