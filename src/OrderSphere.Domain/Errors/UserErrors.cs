using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Domain.Errors;

public static class UserErrors
{
    public static readonly Error NotFound =
        new("User.NotFound", "User was not found.");

    public static readonly Error UpdateFailed =
        new("User.UpdateFailed", "Failed to update user.");
}
