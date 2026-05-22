using OrderSphere.BuildingBlocks.Abstraction;
using System.Text.RegularExpressions;

namespace OrderSphere.Catalog.Domain.Entities;

public sealed class Product : AuditableEntity
{
    public string Name { get; private set; }
    public string Slug { get; private set; }
    public string Description { get; private set; }
    public decimal Price { get; private set; }
    public int Stock { get; private set; }
    public Guid CategoryId { get; private set; }
    public string SKU { get; private set; }
    public string? ImageUrl { get; private set; }
    public bool IsActive { get; private set; } = true;

    public Category? Category { get; set; }

    public Product()
    {
        
    }

    public Product(string name, string description, decimal price, int stock, Guid categoryId, string sku, string? imageUrl = null)
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

    private static string GenerateSlug(string name)
        => Regex.Replace(name.ToLowerInvariant().Trim(), @"[^a-z0-9]+", "-").Trim('-');

    public void AddToStock(int quantity) => Stock += quantity;

    public void RemoveFromStock(int quantity)
    {
        if (quantity > Stock)
            throw new InvalidOperationException("Insufficient stock.");
        Stock -= quantity;
    }

    public void UpdateDetails(string name, string description, decimal price, int stock, Guid categoryId, string sku, string? imageUrl = null)
    {
        Name = name;
        Slug = GenerateSlug(name);
        Description = description;
        Price = price;
        Stock = stock;
        CategoryId = categoryId;
        SKU = sku;
        ImageUrl = imageUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate() { IsActive = true; UpdatedAt = DateTime.UtcNow; }
    public void Deactivate() { IsActive = false; UpdatedAt = DateTime.UtcNow; }
}
