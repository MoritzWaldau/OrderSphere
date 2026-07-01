using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Security;
using OrderSphere.Ordering.Infrastructure.Persistence;

namespace OrderSphere.Ordering.Application.Tests.Helpers;

/// <summary>
/// Creates an isolated <see cref="OrderingDbContext"/> backed by a SQLite in-memory database.
/// SQLite is used rather than the EF in-memory provider because the latter does not reliably
/// apply global query filters on entities with complex properties (e.g. <c>OrderItem.Price</c>),
/// which these tests rely on to verify soft-delete exclusion.
/// </summary>
internal static class OrderingDbContextFactory
{
    internal static OrderingDbContext Create() => Create(NullCurrentUser.Instance);

    internal static OrderingDbContext Create(ICurrentUser currentUser)
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<OrderingDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new OrderingDbContext(options, NullPublisher.Instance, currentUser);
        context.Database.EnsureCreated();
        return context;
    }
}
