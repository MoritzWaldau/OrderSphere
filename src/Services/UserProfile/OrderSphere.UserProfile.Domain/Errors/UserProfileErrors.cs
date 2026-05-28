using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.UserProfile.Domain.Errors;

public static class UserProfileErrors
{
    public static readonly Error ProfileNotFound =
        new("UserProfile.NotFound", "Customer profile not found.");

    public static readonly Error AddressNotFound =
        new("UserProfile.Address.NotFound", "Saved address not found.");

    public static readonly Error AddressLimitExceeded =
        new("UserProfile.Address.LimitExceeded", "Maximum number of saved addresses reached.");

    public static readonly Error UnknownError =
        new("UserProfile.UnknownError", "An unexpected error occurred.");
}
