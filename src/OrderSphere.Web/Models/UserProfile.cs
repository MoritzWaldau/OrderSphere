namespace OrderSphere.Web.Models;

public sealed record ProfileDto(
    Guid Id,
    string KeycloakSubject,
    string DisplayName,
    string Email,
    bool DarkModeEnabled,
    List<AddressDto> Addresses);

public sealed record AddressDto(
    Guid Id,
    string Label,
    string FirstName,
    string LastName,
    string Street,
    string City,
    string PostalCode,
    string Country,
    bool IsDefault);

public sealed record UpdateProfileRequest(string DisplayName, string Email);
public sealed record UpdatePreferencesRequest(bool DarkModeEnabled);
public sealed record CreateAddressRequest(
    string Label,
    string FirstName,
    string LastName,
    string Street,
    string City,
    string PostalCode,
    string Country,
    bool SetAsDefault);
