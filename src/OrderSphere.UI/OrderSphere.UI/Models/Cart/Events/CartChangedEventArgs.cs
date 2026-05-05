using OrderSphere.Application.Models;

namespace OrderSphere.UI.Models.Cart.Events;

public sealed class CartChangedEventArgs
{
    public Guid CustomerId { get; init; }
    public CartDto? Cart { get; init; }
}
