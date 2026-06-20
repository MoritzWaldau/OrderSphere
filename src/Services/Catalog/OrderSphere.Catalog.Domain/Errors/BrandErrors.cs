using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Catalog.Domain.Errors;

public static class BrandErrors
{
    public static readonly Error NotFound = new("Brand.NotFound", "Brand was not found.", ErrorType.NotFound);
    public static readonly Error NameAlreadyExists = new("Brand.NameAlreadyExists", "A brand with this name already exists.", ErrorType.Conflict);
    public static readonly Error HasProducts = new("Brand.HasProducts", "Cannot delete a brand that has products assigned to it.", ErrorType.Conflict);
    public static readonly Error UnknownError = new("Brand.UnknownError", "An unexpected error occurred.", ErrorType.Unexpected);
}
