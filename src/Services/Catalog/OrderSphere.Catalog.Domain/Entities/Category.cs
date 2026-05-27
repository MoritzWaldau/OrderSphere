using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.StronglyTypedIds;

namespace OrderSphere.Catalog.Domain.Entities;

public sealed class Category : AuditableEntity<CategoryId>, IAggregateRoot
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Parameterless constructor for EF Core materialisation.
    private Category()
    {
        Name = string.Empty;
        Description = string.Empty;
    }

    public Category(string name, string description = "")
    {
        Id = CategoryId.New();
        Name = name;
        Description = description;
    }

    public void Activate()   { IsActive = true;  UpdatedAt = DateTime.UtcNow; }
    public void Deactivate() { IsActive = false; UpdatedAt = DateTime.UtcNow; }

    public void UpdateDetails(string name, string description)
    {
        Name = name;
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }
}
