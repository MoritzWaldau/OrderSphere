namespace OrderSphere.Domain.Abstraction;

public interface IEntity
{
    Guid Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public bool IsDeleted { get; set; }
}
