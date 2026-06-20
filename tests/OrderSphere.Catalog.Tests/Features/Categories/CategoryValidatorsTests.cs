using OrderSphere.Catalog.Application.Features.Categories.Admin.CreateCategory;
using OrderSphere.Catalog.Application.Features.Categories.Admin.DeleteCategory;
using OrderSphere.Catalog.Application.Features.Categories.Admin.UpdateCategory;

namespace OrderSphere.Catalog.Tests.Features.Categories;

public sealed class CreateCategoryCommandValidatorTests
{
    private readonly CreateCategoryCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_Passes()
        => _validator.Validate(new CreateCategoryCommand("Shoes", "Footwear")).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_EmptyName_Fails()
        => _validator.Validate(new CreateCategoryCommand("", "desc")).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_NameTooLong_Fails()
        => _validator.Validate(new CreateCategoryCommand(new string('x', 101), "desc")).IsValid.Should().BeFalse();
}

public sealed class DeleteCategoryCommandValidatorTests
{
    private readonly DeleteCategoryCommandValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyId_Passes()
        => _validator.Validate(new DeleteCategoryCommand(CategoryId.New())).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_EmptyId_Fails()
        => _validator.Validate(new DeleteCategoryCommand(default)).IsValid.Should().BeFalse();
}

public sealed class UpdateCategoryCommandValidatorTests
{
    private readonly UpdateCategoryCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_Passes()
        => _validator.Validate(new UpdateCategoryCommand(CategoryId.New(), "Shoes", "desc", true)).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_EmptyId_Fails()
        => _validator.Validate(new UpdateCategoryCommand(default, "Shoes", "desc", true)).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyName_Fails()
        => _validator.Validate(new UpdateCategoryCommand(CategoryId.New(), "", "desc", true)).IsValid.Should().BeFalse();
}
