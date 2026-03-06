using System.ComponentModel.DataAnnotations;

namespace OrderSphere.Domain.Abstraction;

public abstract class Entity : IEntity
{
    [Key]
    public Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public bool IsDeleted { get; set; }
}
