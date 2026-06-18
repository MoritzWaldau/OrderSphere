namespace OrderSphere.Catalog.Domain.Enums;

public enum ReviewStatus
{
    /// <summary>Visible to the public and counted in the product rating summary.</summary>
    Approved = 0,

    /// <summary>Hidden by a moderator; excluded from public listing and rating summary.</summary>
    Rejected = 1,
}
