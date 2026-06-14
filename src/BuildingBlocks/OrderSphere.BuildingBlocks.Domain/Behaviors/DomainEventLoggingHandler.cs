using MediatR;
using Microsoft.Extensions.Logging;
using OrderSphere.BuildingBlocks.Abstraction;

namespace OrderSphere.BuildingBlocks.Behaviors;

/// <summary>
/// Open-generic notification handler that logs every domain event at Debug level.
/// Register via: <c>services.AddTransient(typeof(INotificationHandler&lt;&gt;), typeof(DomainEventLoggingHandler&lt;&gt;));</c>
/// The generic constraint ensures it is instantiated only for <see cref="IDomainEvent"/> types.
/// </summary>
public sealed class DomainEventLoggingHandler<TDomainEvent>(
    ILogger<DomainEventLoggingHandler<TDomainEvent>> logger)
    : INotificationHandler<TDomainEvent>
    where TDomainEvent : class, IDomainEvent
{
    public Task Handle(TDomainEvent notification, CancellationToken cancellationToken)
    {
        // Log the event type only — never the full payload, which can carry PII
        // (customer names, emails, addresses).
        logger.LogDebug("Domain event raised: {EventType}", typeof(TDomainEvent).Name);

        return Task.CompletedTask;
    }
}
