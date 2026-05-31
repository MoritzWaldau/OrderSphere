using MediatR;

namespace OrderSphere.BuildingBlocks.Abstraction;

/// <summary>
/// Marker interface for in-process domain events dispatched via MediatR.
/// Domain events represent facts that occurred within an aggregate boundary
/// and are dispatched after <see cref="Microsoft.EntityFrameworkCore.DbContext.SaveChangesAsync"/> succeeds.
/// </summary>
public interface IDomainEvent : INotification { }
