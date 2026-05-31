using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.UserProfile.Domain.Errors;

public static class UserProfileErrors
{
    public static readonly Error ProfileNotFound =
        new("UserProfile.NotFound", "Customer profile not found.", ErrorType.NotFound);

    public static readonly Error AddressNotFound =
        new("UserProfile.Address.NotFound", "Saved address not found.", ErrorType.NotFound);

    public static readonly Error AddressLimitExceeded =
        new("UserProfile.Address.LimitExceeded", "Maximum number of saved addresses reached.", ErrorType.Conflict);
}
