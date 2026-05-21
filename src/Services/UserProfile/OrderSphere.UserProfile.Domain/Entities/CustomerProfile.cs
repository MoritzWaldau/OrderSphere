using OrderSphere.BuildingBlocks.Abstraction;

namespace OrderSphere.UserProfile.Domain.Entities;

/// <summary>
/// Stores user data that doesn't belong in Keycloak:
/// saved shipping addresses and UI preferences.
/// Keycloak subject claim is the external identifier.
/// </summary>
public sealed class CustomerProfile : AuditableEntity
{
    public string KeycloakSubject { get; private set; }
    public string DisplayName { get; private set; }
    public string Email { get; private set; }
    public bool DarkModeEnabled { get; private set; }

    private readonly List<SavedAddress> _addresses = [];
    public IReadOnlyList<SavedAddress> Addresses => _addresses.AsReadOnly();

    private CustomerProfile()
    {
        KeycloakSubject = string.Empty;
        DisplayName = string.Empty;
        Email = string.Empty;
    }

    public CustomerProfile(string keycloakSubject, string displayName, string email)
    {
        KeycloakSubject = keycloakSubject;
        DisplayName = displayName;
        Email = email;
        CreatedAt = DateTime.UtcNow;
    }

    public void UpdateDetails(string displayName, string email)
    {
        DisplayName = displayName;
        Email = email;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetDarkMode(bool enabled)
    {
        DarkModeEnabled = enabled;
        UpdatedAt = DateTime.UtcNow;
    }

    public SavedAddress AddAddress(
        string label, string firstName, string lastName,
        string street, string city, string postalCode, string country,
        bool setAsDefault = false)
    {
        if (setAsDefault)
        {
            foreach (var existing in _addresses)
                existing.ClearDefault();
        }

        var address = new SavedAddress(
            Id, label, firstName, lastName,
            street, city, postalCode, country,
            isDefault: setAsDefault || _addresses.Count == 0);

        _addresses.Add(address);
        UpdatedAt = DateTime.UtcNow;
        return address;
    }

    public bool RemoveAddress(Guid addressId)
    {
        var address = _addresses.FirstOrDefault(a => a.Id == addressId);
        if (address is null) return false;

        _addresses.Remove(address);

        // If the removed address was the default, promote the first remaining one.
        if (address.IsDefault && _addresses.Count > 0)
            _addresses[0].SetAsDefault();

        UpdatedAt = DateTime.UtcNow;
        return true;
    }

    public bool SetDefaultAddress(Guid addressId)
    {
        var target = _addresses.FirstOrDefault(a => a.Id == addressId);
        if (target is null) return false;

        foreach (var a in _addresses)
            a.ClearDefault();

        target.SetAsDefault();
        UpdatedAt = DateTime.UtcNow;
        return true;
    }
}
