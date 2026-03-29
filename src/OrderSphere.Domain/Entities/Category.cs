using OrderSphere.Domain.Abstraction;

namespace OrderSphere.Domain.Entities;

public sealed class Category(string name, string description = "") : AuditableEntity
{
    public string Name { get; private set; } = name;
    public string Description { get; private set; } = description;
    public bool IsActive { get; private set; } = true;

    public void Deactivate()
    {
        IsActive = false;
    }

    public void Activate()
    {
        IsActive = true;
    }
}
