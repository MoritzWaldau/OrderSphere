using OrderSphere.Domain.Abstraction;
using System.ComponentModel.DataAnnotations.Schema;

namespace OrderSphere.Domain.Entities;

public sealed class Product(string name, string description, decimal price, int stock) : Entity
{
    public string Name { get; private set; } = name;
    public string Description { get; private set; } = description;
    public decimal Price { get; private set; } = price;
    public int Stock { get; private set; } = stock;

    public void ReduceStock(int quantity)
    {
        if (Stock < quantity)
            throw new Exception("Not enough stock");

        Stock -= quantity;
    }
}
