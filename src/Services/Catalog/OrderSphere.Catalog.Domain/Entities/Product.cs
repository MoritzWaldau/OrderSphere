using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.BuildingBlocks.ValueObjects;
using OrderSphere.Catalog.Domain.DomainEvents;
using OrderSphere.Catalog.Domain.Errors;
using System.Text.RegularExpressions;

namespace OrderSphere.Catalog.Domain.Entities;

public sealed class Product : AuditableEntity<ProductId>, IAggregateRoot
{
    public string Name { get; private set; } = null!;
    public string Slug { get; private set; } = null!;
    public string Description { get; private set; } = null!;
    public Money Price { get; private set; } = null!;
    public int Stock { get; private set; }
    public CategoryId CategoryId { get; private set; }
    public string SKU { get; private set; } = null!;
    public string? ImageUrl { get; private set; }
    public bool IsActive { get; private set; } = true;

    public Category? Category { get; set; }

    // Parameterless constructor for EF Core materialisation.
    public Product() { }

    public Product(string name, string description, Money price, int stock, CategoryId categoryId, string sku, string? imageUrl = null)
    {
        Id = ProductId.New();
        Name = name;
        Slug = GenerateSlug(name);
        Description = description;
        Price = price;
        Stock = stock;
        CategoryId = categoryId;
        SKU = sku;
        ImageUrl = imageUrl;
    }

    private static string GenerateSlug(string name)
        => Regex.Replace(name.ToLowerInvariant().Trim(), @"[^a-z0-9]+", "-").Trim('-');

    public void AddToStock(int quantity) => Stock += quantity;

    public Result RemoveFromStock(int quantity)
    {
        if (quantity > Stock)
            return Result.Failure(ProductErrors.InsufficientStock);
        Stock -= quantity;
        RaiseDomainEvent(new ProductStockDecreasedDomainEvent(Id, quantity));
        return Result.Success();
    }

    public void UpdateDetails(string name, string description, Money price, int stock, CategoryId categoryId, string sku, string? imageUrl = null)
    {
        Name = name;
        Slug = GenerateSlug(name);
        Description = description;
        Price = price;
        Stock = stock;
        CategoryId = categoryId;
        SKU = sku;
        ImageUrl = imageUrl;
    }

    public void Activate()
    {
        IsActive = true;
        RaiseDomainEvent(new ProductActivatedDomainEvent(Id));
    }

    public void Deactivate()
    {
        IsActive = false;
        RaiseDomainEvent(new ProductDeactivatedDomainEvent(Id));
    }

    public void Delete()
    {
        IsDeleted = true;
        RaiseDomainEvent(new ProductDeletedDomainEvent(Id));
    }
}
