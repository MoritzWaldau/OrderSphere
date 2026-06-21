namespace OrderSphere.Ordering.Domain.Enums;

/// <summary>
/// High-level state of the checkout-to-payment process for a single correlation id,
/// projected from the integration events the Ordering worker already processes.
/// This is an observability read-model, not the authoritative order state.
/// </summary>
public enum SagaState
{
    /// <summary>Order persisted from an accepted checkout (stock already reserved upstream).</summary>
    OrderCreated,

    /// <summary>Payment requested; awaiting the payment result.</summary>
    PaymentRequested,

    /// <summary>Payment succeeded, reservation confirmed, order confirmed. Terminal.</summary>
    Confirmed,

    /// <summary>Payment failed; order cancelled and reservation released. Terminal.</summary>
    Cancelled,

    /// <summary>
    /// Payment succeeded but order/reservation confirmation failed after bounded retries.
    /// A refund has been requested; awaiting confirmation. Non-terminal compensation state.
    /// </summary>
    CompensationPending,

    /// <summary>
    /// Payment was refunded after a failed confirmation; the compensation loop is closed. Terminal.
    /// </summary>
    Refunded
}
