using OrderSphere.Domain.Abstraction;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderSphere.Domain.Entities;

public sealed class Product : AuditableEntity
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public decimal Price { get; private set; }
    public int Stock { get; private set; }
    public Guid CategoryId { get; private set; }
    public string SKU { get; private set; }
    public bool IsActive { get; private set; } = true;

    public Category? Category { get; set; }

    // Primärer Konstruktor - mit Kategorie und SKU
    public Product(string name, string description, decimal price, int stock, Guid categoryId, string sku)
    {
        Name = name;
        Description = description;
        Price = price;
        Stock = stock;
        CategoryId = categoryId;
        SKU = sku;
    }

    // Backward-Kompatibilität - ohne Kategorie und SKU
    public Product(string name, string description, decimal price, int stock)
    {
        Name = name;
        Description = description;
        Price = price;
        Stock = stock;
        CategoryId = Guid.Empty;
        SKU = string.Empty;
    }

    public void AddToStock(int quantity)
    {
        if (quantity < 0 && Math.Abs(quantity) > Stock)
            throw new InvalidOperationException("Not enough stock");

        Stock += quantity;
    }

    public void RemoveFromStock(int quantity)
    {
        if (quantity < 0 && Math.Abs(quantity) > Stock)
            throw new InvalidOperationException("Not enough stock");

        Stock -= quantity;
    }
}
