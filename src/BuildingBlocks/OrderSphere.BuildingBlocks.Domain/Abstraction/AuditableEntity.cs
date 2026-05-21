using System.ComponentModel.DataAnnotations;

namespace OrderSphere.BuildingBlocks.Abstraction;

public abstract class AuditableEntity : IAuditableEntity
{
    [Key]
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
