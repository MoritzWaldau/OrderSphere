namespace OrderSphere.BuildingBlocks.Abstraction;

/// <summary>
/// Implemented by <see cref="AuditableEntity{TId}"/>. Exposes accumulated domain events
/// and clears them atomically so a single caller (the DbContext) can drain them once per save.
/// </summary>
public interface IHasDomainEvents
{
    /// <summary>
    /// Returns all pending domain events and clears the internal collection.
    /// Calling this twice on the same instance returns an empty list on the second call.
    /// </summary>
    IReadOnlyList<IDomainEvent> PopDomainEvents();
}
