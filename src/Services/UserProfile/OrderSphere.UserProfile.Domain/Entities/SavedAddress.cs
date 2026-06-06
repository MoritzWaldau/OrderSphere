using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.StronglyTypedIds;

namespace OrderSphere.UserProfile.Domain.Entities;

public sealed class SavedAddress : AuditableEntity<SavedAddressId>
{
    public CustomerProfileId CustomerProfileId { get; private set; }
    public string Label { get; private set; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string Street { get; private set; }
    public string City { get; private set; }
    public string PostalCode { get; private set; }
    public string Country { get; private set; }
    public bool IsDefault { get; private set; }

    // Parameterless constructor for EF Core materialisation.
    private SavedAddress()
    {
        CustomerProfileId = CustomerProfileId.Empty;
        Label = string.Empty;
        FirstName = string.Empty;
        LastName = string.Empty;
        Street = string.Empty;
        City = string.Empty;
        PostalCode = string.Empty;
        Country = string.Empty;
    }

    internal SavedAddress(
        CustomerProfileId customerProfileId,
        string label, string firstName, string lastName,
        string street, string city, string postalCode, string country,
        bool isDefault)
    {
        Id = SavedAddressId.New();
        CustomerProfileId = customerProfileId;
        Label = label;
        FirstName = firstName;
        LastName = lastName;
        Street = street;
        City = city;
        PostalCode = postalCode;
        Country = country;
        IsDefault = isDefault;
    }

    public void Update(
        string label, string firstName, string lastName,
        string street, string city, string postalCode, string country)
    {
        Label = label;
        FirstName = firstName;
        LastName = lastName;
        Street = street;
        City = city;
        PostalCode = postalCode;
        Country = country;
    }

    internal void SetAsDefault() => IsDefault = true;
    internal void ClearDefault() => IsDefault = false;
    internal void MarkDeleted() => IsDeleted = true;
}
