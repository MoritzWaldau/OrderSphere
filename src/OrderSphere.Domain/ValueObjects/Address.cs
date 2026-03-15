namespace OrderSphere.Domain.ValueObjects;

public sealed class Address
{
    public string FirstName { get; }
    public string LastName { get; }
    public string Street { get; }
    public string City { get; }
    public string PostalCode { get; }
    public string Country { get; }

    public Address() { }

    public Address(
        string firstName,
        string lastName,
        string street,
        string city,
        string postalCode,
        string country)
    {
        FirstName = firstName;
        LastName = lastName;
        Street = street;
        City = city;
        PostalCode = postalCode;
        Country = country;
    }
}
