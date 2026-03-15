using OrderSphere.Domain.Abstraction;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderSphere.Domain.Entities;

public sealed class Product(string name, string description, decimal price, int stock) : AuditableEntity
{
    public string Name { get; private set; } = name;
    public string Description { get; private set; } = description;
    public decimal Price { get; private set; } = price;
    public int Stock { get; private set; } = stock;

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
