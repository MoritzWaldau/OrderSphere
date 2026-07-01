using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.StronglyTypedIds;

namespace OrderSphere.UserProfile.Domain.Entities;

/// <summary>
/// Stores user data that doesn't belong in the identity provider (Auth0):
/// saved shipping addresses and UI preferences.
/// The Auth0 subject claim is the external identifier.
/// </summary>
public sealed class CustomerProfile : AuditableEntity<CustomerProfileId>, IAggregateRoot
{
    /// <summary>Maximum number of active saved addresses a profile may hold.</summary>
    public const int MaxAddresses = 10;

    public string Subject { get; private set; }
    public string DisplayName { get; private set; }
    public string Email { get; private set; }
    public bool DarkModeEnabled { get; private set; }
    public bool IsOnboardingComplete { get; private set; }
    public NotificationPreferences NotificationPreferences { get; private set; } = NotificationPreferences.Default();

    private readonly List<SavedAddress> _addresses = [];
    public IReadOnlyList<SavedAddress> Addresses => _addresses.AsReadOnly();

    private CustomerProfile()
    {
        Subject = string.Empty;
        DisplayName = string.Empty;
        Email = string.Empty;
        NotificationPreferences = NotificationPreferences.Default();
    }

    public CustomerProfile(string subject, string displayName, string email)
    {
        Id = CustomerProfileId.New();
        Subject = subject;
        DisplayName = displayName;
        Email = email;
        NotificationPreferences = NotificationPreferences.Default();
    }

    public void MarkOnboardingComplete()
    {
        IsOnboardingComplete = true;
    }

    public void UpdateDetails(string displayName, string email)
    {
        DisplayName = displayName;
        Email = email;
    }

    public void SetDarkMode(bool enabled)
    {
        DarkModeEnabled = enabled;
    }

    public void UpdateNotificationPreferences(bool emailEnabled, bool smsEnabled, bool pushEnabled, DateTime nowUtc)
    {
        NotificationPreferences.Update(emailEnabled, smsEnabled, pushEnabled, nowUtc);
    }

    /// <summary>
    /// GDPR right-to-erasure: overwrites PII with a placeholder and hard-deletes saved addresses.
    /// The row itself is kept (soft-deleted via <see cref="AuditableEntity{TId}.IsDeleted"/>) so
    /// foreign keys from other aggregates in this service stay valid.
    /// </summary>
    public void Anonymize()
    {
        DisplayName = "Erased customer";
        Email = $"erased-{Id.Value}@erased.invalid";
        foreach (var address in _addresses)
            address.Anonymize();
        IsDeleted = true;
    }

    public SavedAddress AddAddress(
        string label, string firstName, string lastName,
        string street, string city, string postalCode, string country,
        bool setAsDefault = false)
    {
        if (setAsDefault)
        {
            foreach (var existing in _addresses.Where(a => !a.IsDeleted))
                existing.ClearDefault();
        }

        var hasActiveAddress = _addresses.Any(a => !a.IsDeleted);
        var address = new SavedAddress(
            Id, label, firstName, lastName,
            street, city, postalCode, country,
            isDefault: setAsDefault || !hasActiveAddress);

        _addresses.Add(address);
        return address;
    }

    public bool RemoveAddress(SavedAddressId addressId)
    {
        var address = _addresses.FirstOrDefault(a => a.Id == addressId && !a.IsDeleted);
        if (address is null) return false;

        var wasDefault = address.IsDefault;
        address.ClearDefault();
        address.MarkDeleted();

        // If the removed address was the default, promote the first remaining active one.
        if (wasDefault)
            _addresses.FirstOrDefault(a => !a.IsDeleted)?.SetAsDefault();

        return true;
    }

    public bool SetDefaultAddress(SavedAddressId addressId)
    {
        var target = _addresses.FirstOrDefault(a => a.Id == addressId && !a.IsDeleted);
        if (target is null) return false;

        foreach (var a in _addresses.Where(a => !a.IsDeleted))
            a.ClearDefault();

        target.SetAsDefault();
        return true;
    }
}
