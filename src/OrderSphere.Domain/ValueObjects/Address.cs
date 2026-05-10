using System.Text.Json.Serialization;

namespace OrderSphere.Domain.ValueObjects;

public sealed class Address
{
    public string FirstName { get; }
    public string LastName { get; }
    public string Street { get; }
    public string City { get; }
    public string PostalCode { get; }
    public string Country { get; }

    private Address()
    {
        FirstName = null!;
        LastName = null!;
        Street = null!;
        City = null!;
        PostalCode = null!;
        Country = null!;
    }

    [JsonConstructor]
    public Address(
        string firstName,
        string lastName,
        string street,
        string city,
        string postalCode,
        string country)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(firstName);
        ArgumentException.ThrowIfNullOrWhiteSpace(lastName);
        ArgumentException.ThrowIfNullOrWhiteSpace(street);
        ArgumentException.ThrowIfNullOrWhiteSpace(city);
        ArgumentException.ThrowIfNullOrWhiteSpace(postalCode);
        ArgumentException.ThrowIfNullOrWhiteSpace(country);

        FirstName = firstName;
        LastName = lastName;
        Street = street;
        City = city;
        PostalCode = postalCode;
        Country = country;
    }
}
