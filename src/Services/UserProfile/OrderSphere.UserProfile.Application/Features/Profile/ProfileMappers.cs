namespace OrderSphere.UserProfile.Application.Features.Profile;

internal static class ProfileMappers
{
    internal static ProfileDto ToDto(CustomerProfile p) => new(
        p.Id.Value,
        p.Subject,
        p.DisplayName,
        p.Email,
        p.DarkModeEnabled,
        p.Addresses.Select(ToAddressDto).ToList());

    internal static AddressDto ToAddressDto(SavedAddress a) => new(
        a.Id.Value,
        a.Label,
        a.FirstName,
        a.LastName,
        a.Street,
        a.City,
        a.PostalCode,
        a.Country,
        a.IsDefault);
}
