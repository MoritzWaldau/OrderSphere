namespace OrderSphere.Catalog.Domain.Enums;

public enum ReservationStatus
{
    /// <summary>Holds availability against the product; not yet decremented from on-hand stock.</summary>
    Active = 0,

    /// <summary>Payment succeeded; on-hand stock has been decremented and the hold consumed.</summary>
    Confirmed = 1,

    /// <summary>Released (payment failed/cancelled or expired); no longer holds availability.</summary>
    Released = 2,
}
