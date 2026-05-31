namespace OrderSphere.BuildingBlocks.Abstraction;

/// <summary>
/// Marker interface for audit fields. Does not include Id — each entity
/// declares its primary key via <see cref="AuditableEntity{TId}"/>.
/// </summary>
public interface IAuditableEntity
{
    DateTime CreatedAt { get; set; }
    DateTime? UpdatedAt { get; set; }
    bool IsDeleted { get; set; }
}
