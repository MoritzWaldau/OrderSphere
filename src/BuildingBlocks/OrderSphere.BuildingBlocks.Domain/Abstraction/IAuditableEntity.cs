namespace OrderSphere.Domain.Abstraction;

public interface IAuditableEntity
{
    Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
}
