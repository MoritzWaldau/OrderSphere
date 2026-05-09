using OrderSphere.Domain.Entities;

namespace OrderSphere.Domain.Tests.Entities;

public class ProductTests
{
    private static Product CreateProduct(int stock = 10) =>
        new("Test Product", "Description", 9.99m, stock, Guid.NewGuid(), "TEST-SKU");

    [Fact]
    public void Constructor_GeneratesSlugFromName()
    {
        var product = new Product("MacBook Pro 16\"", "Desc", 1m, 1, Guid.NewGuid(), "SKU");

        product.Slug.Should().Be("macbook-pro-16");
    }

    [Fact]
    public void Constructor_DefaultsToActive()
    {
        var product = CreateProduct();

        product.IsActive.Should().BeTrue();
    }

    [Fact]
    public void RemoveFromStock_WithSufficientStock_DecrementsStock()
    {
        var product = CreateProduct(stock: 10);

        product.RemoveFromStock(3);

        product.Stock.Should().Be(7);
    }

    [Fact]
    public void AddToStock_IncrementsStock()
    {
        var product = CreateProduct(stock: 5);

        product.AddToStock(3);

        product.Stock.Should().Be(8);
    }

    [Fact]
    public void Deactivate_SetsIsActiveFalseAndUpdatesTimestamp()
    {
        var product = CreateProduct();

        product.Deactivate();

        product.IsActive.Should().BeFalse();
        product.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateDetails_ChangesAllFieldsAndRegeneratesSlug()
    {
        var product = CreateProduct();
        var newCategoryId = Guid.NewGuid();

        product.UpdateDetails("iPhone 16", "New phone", 999m, 50, newCategoryId, "NEW-SKU");

        product.Name.Should().Be("iPhone 16");
        product.Slug.Should().Be("iphone-16");
        product.Description.Should().Be("New phone");
        product.Price.Should().Be(999m);
        product.Stock.Should().Be(50);
        product.CategoryId.Should().Be(newCategoryId);
        product.SKU.Should().Be("NEW-SKU");
        product.UpdatedAt.Should().NotBeNull();
    }
}
