using MediatR;

namespace OrderSphere.BuildingBlocks.Abstraction;

/// <summary>
/// No-op <see cref="IPublisher"/> used exclusively by EF Core design-time factories
/// (<c>IDesignTimeDbContextFactory</c>) where a real DI container is unavailable.
/// Must not be registered in production DI.
/// </summary>
public sealed class NullPublisher : IPublisher
{
    public static readonly NullPublisher Instance = new();

    private NullPublisher() { }

    public Task Publish(object notification, CancellationToken cancellationToken = default)
        => Task.CompletedTask;

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
        => Task.CompletedTask;
}
