using System.ComponentModel.DataAnnotations;

namespace OrderSphere.BuildingBlocks.Abstraction;

/// <summary>
/// Base class for all domain entities. <typeparamref name="TId"/> is a strongly-typed
/// ID struct (e.g. <c>ProductId</c>) that wraps a <see cref="Guid"/> value.
/// EF maps the typed ID to a UUID column via <c>ConfigureConventions</c> in each DbContext.
/// </summary>
public abstract class AuditableEntity<TId> : IAuditableEntity
{
    [Key]
    public TId Id { get; set; } = default!;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
