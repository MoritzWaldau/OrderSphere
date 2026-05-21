using OrderSphere.BuildingBlocks.Abstraction;

namespace OrderSphere.Catalog.Domain.Entities;

public sealed class Category(string name, string description = "") : AuditableEntity
{
    public string Name { get; private set; } = name;
    public string Description { get; private set; } = description;
    public bool IsActive { get; private set; } = true;

    public void Activate() { IsActive = true; UpdatedAt = DateTime.UtcNow; }
    public void Deactivate() { IsActive = false; UpdatedAt = DateTime.UtcNow; }

    public void UpdateDetails(string name, string description)
    {
        Name = name;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }
}
