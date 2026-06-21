using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace OrderSphere.IntegrationTests.Api;

/// <summary>
/// Creates the <typeparamref name="TContext"/> schema from the EF model on host start, so the
/// in-memory SQLite database is ready before the first request. Registered after the production
/// hosted services are stripped, so it is the only background service that runs.
/// </summary>
internal sealed class SqliteSchemaInitializer<TContext>(IServiceProvider services) : IHostedService
    where TContext : DbContext
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TContext>();
        await context.Database.EnsureCreatedAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
