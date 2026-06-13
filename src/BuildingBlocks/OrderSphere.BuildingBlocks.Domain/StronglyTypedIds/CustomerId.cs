namespace OrderSphere.BuildingBlocks.StronglyTypedIds;

public readonly record struct CustomerId(Guid Value)
{
    public static CustomerId New() => new(Guid.CreateVersion7());
    public static CustomerId Empty => new(Guid.Empty);
    public static CustomerId From(Guid v) => new(v);

    // Auth0 sub format is "auth0|<opaque_id>", not a UUID. Derive a stable,
    // deterministic GUID via SHA256 so the identity round-trips consistently.
    public static CustomerId FromSub(string sub)
    {
        var hash = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(sub));
        hash[6] = (byte)((hash[6] & 0x0F) | 0x50); // RFC 4122 version 5
        hash[8] = (byte)((hash[8] & 0x3F) | 0x80); // RFC 4122 variant
        return new CustomerId(new Guid(hash[..16]));
    }

    public override string ToString() => Value.ToString();
}
