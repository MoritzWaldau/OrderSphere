namespace OrderSphere.Catalog.Application.Features.Brands.Admin.CreateBrand;

public sealed record CreateBrandCommand(string Name, string Description, string? LogoUrl = null)
    : ICommand<Result<Guid>>;
