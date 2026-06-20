using OrderSphere.Catalog.Application.Features.Brands.Admin.CreateBrand;
using OrderSphere.Catalog.Application.Features.Brands.Admin.DeleteBrand;
using OrderSphere.Catalog.Application.Features.Brands.Admin.UpdateBrand;

namespace OrderSphere.Catalog.Tests.Features.Brands;

public sealed class CreateBrandCommandValidatorTests
{
    private readonly CreateBrandCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_Passes()
        => _validator.Validate(new CreateBrandCommand("Nike", "Sportswear", null)).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_EmptyName_Fails()
        => _validator.Validate(new CreateBrandCommand("", "desc", null)).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_NameTooLong_Fails()
        => _validator.Validate(new CreateBrandCommand(new string('x', 101), "desc", null)).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_LogoUrlTooLong_Fails()
        => _validator.Validate(new CreateBrandCommand("Nike", "desc", new string('x', 501))).IsValid.Should().BeFalse();
}

public sealed class DeleteBrandCommandValidatorTests
{
    private readonly DeleteBrandCommandValidator _validator = new();

    [Fact]
    public void Validate_NonEmptyId_Passes()
        => _validator.Validate(new DeleteBrandCommand(BrandId.New())).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_EmptyId_Fails()
        => _validator.Validate(new DeleteBrandCommand(default)).IsValid.Should().BeFalse();
}

public sealed class UpdateBrandCommandValidatorTests
{
    private readonly UpdateBrandCommandValidator _validator = new();

    [Fact]
    public void Validate_ValidCommand_Passes()
        => _validator.Validate(new UpdateBrandCommand(BrandId.New(), "Nike", "desc", null, true)).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_EmptyId_Fails()
        => _validator.Validate(new UpdateBrandCommand(default, "Nike", "desc", null, true)).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_EmptyName_Fails()
        => _validator.Validate(new UpdateBrandCommand(BrandId.New(), "", "desc", null, true)).IsValid.Should().BeFalse();
}
