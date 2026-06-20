using System.Text.RegularExpressions;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Catalog.Domain.DomainEvents;

namespace OrderSphere.Catalog.Domain.Entities;

public sealed class Brand : AuditableEntity<BrandId>, IAggregateRoot
{
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public string? LogoUrl { get; private set; }
    public string? LogoBlobName { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Parameterless constructor for EF Core materialisation.
    private Brand() { }

    public Brand(string name, string description = "", string? logoUrl = null)
    {
        Id = BrandId.New();
        Name = name;
        Slug = GenerateSlug(name);
        Description = description;
        LogoUrl = logoUrl;
    }

    private static string GenerateSlug(string name)
        => Regex.Replace(name.ToLowerInvariant().Trim(), @"[^a-z0-9]+", "-").Trim('-');

    public void SetLogoBlob(string? blobName) => LogoBlobName = blobName;

    public void Activate() => IsActive = true;
    public void Deactivate() => IsActive = false;

    public void UpdateDetails(string name, string description, string? logoUrl = null)
    {
        Name = name;
        Slug = GenerateSlug(name);
        Description = description;
        LogoUrl = logoUrl;
    }

    public void Delete()
    {
        IsDeleted = true;
        RaiseDomainEvent(new BrandDeletedDomainEvent(Id));
    }
}
