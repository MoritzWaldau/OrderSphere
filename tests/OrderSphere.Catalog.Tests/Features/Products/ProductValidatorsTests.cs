using OrderSphere.Catalog.Application.Features.Products.Admin.CreateProduct;
using OrderSphere.Catalog.Application.Features.Products.Admin.DeleteProduct;
using OrderSphere.Catalog.Application.Features.Products.Admin.UpdateProduct;
using OrderSphere.Catalog.Application.Features.Products.Admin.UploadProductImage;

namespace OrderSphere.Catalog.Tests.Features.Products;

public sealed class CreateProductCommandValidatorTests
{
    private readonly CreateProductCommandValidator _validator = new();

    private static CreateProductCommand Valid()
        => new("Trail Runner", "desc", 99.99m, 10, CategoryId.New(), "SKU-1", null);

    [Fact]
    public void Validate_ValidCommand_Passes() => _validator.Validate(Valid()).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_EmptyName_Fails()
        => _validator.Validate(Valid() with { Name = "" }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_NonPositivePrice_Fails()
        => _validator.Validate(Valid() with { Price = 0m }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_NegativeStock_Fails()
        => _validator.Validate(Valid() with { Stock = -1 }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptySku_Fails()
        => _validator.Validate(Valid() with { SKU = "" }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyCategoryId_Fails()
        => _validator.Validate(Valid() with { CategoryId = default }).IsValid.Should().BeFalse();
}

public sealed class DeleteProductCommandValidatorTests
{
    private readonly DeleteProductCommandValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyId_Passes()
        => _validator.Validate(new DeleteProductCommand(ProductId.New())).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_EmptyId_Fails()
        => _validator.Validate(new DeleteProductCommand(default)).IsValid.Should().BeFalse();
}

public sealed class UpdateProductCommandValidatorTests
{
    private readonly UpdateProductCommandValidator _validator = new();

    private static UpdateProductCommand Valid()
        => new(ProductId.New(), "Trail Runner", "desc", 99.99m, 10, CategoryId.New(), "SKU-1", true, null);

    [Fact]
    public void Validate_ValidCommand_Passes() => _validator.Validate(Valid()).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_EmptyProductId_Fails()
        => _validator.Validate(Valid() with { ProductId = default }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_NonPositivePrice_Fails()
        => _validator.Validate(Valid() with { Price = -5m }).IsValid.Should().BeFalse();
}

public sealed class UploadProductImageCommandValidatorTests
{
    private readonly UploadProductImageCommandValidator _validator = new();

    private static UploadProductImageCommand Command(string contentType)
        => new(ProductId.New(), Stream.Null, contentType, "photo.png");

    [Fact]
    public void Validate_AllowedContentType_Passes()
        => _validator.Validate(Command("image/png")).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_DisallowedContentType_Fails()
        => _validator.Validate(Command("application/pdf")).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyContentType_Fails()
        => _validator.Validate(Command("")).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyProductId_Fails()
        => _validator.Validate(Command("image/png") with { ProductId = default }).IsValid.Should().BeFalse();
}
