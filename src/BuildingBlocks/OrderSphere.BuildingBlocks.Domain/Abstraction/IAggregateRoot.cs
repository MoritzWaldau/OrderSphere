namespace OrderSphere.BuildingBlocks.Abstraction;

/// <summary>
/// Marker interface for aggregate roots.
/// An aggregate root is the sole entry point for all state mutations
/// within its aggregate boundary and is the only entity that may be
/// directly retrieved from a repository.
/// </summary>
public interface IAggregateRoot { }
