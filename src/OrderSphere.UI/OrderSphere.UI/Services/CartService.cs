using OrderSphere.Application.Models;
using MediatR;
using OrderSphere.Application.Features.Cart.GetCart;

namespace OrderSphere.UI.Services;

public interface ICartService
{
    event Action<CartDto>? OnCartUpdated;
    Task<CartDto?> GetCartAsync(Guid customerId);
    Task RefreshCartAsync(Guid customerId);
    void UpdateCart(CartDto cart);
}

public sealed class CartService : ICartService
{
    private readonly ISender _sender;
    public event Action<CartDto>? OnCartUpdated;

    private CartDto? _currentCart;

    public CartService(ISender sender)
    {
        _sender = sender;
    }

    public Task<CartDto?> GetCartAsync(Guid customerId)
    {
        return Task.FromResult(_currentCart);
    }

    public async Task RefreshCartAsync(Guid customerId)
    {
        try
        {
            var result = await _sender.Send(new GetCartQuery(customerId));
            if (result.IsSuccess)
            {
                UpdateCart(result.Value);
            }
        }
        catch
        {
            // Handle error silently for now
        }
    }

    public void UpdateCart(CartDto cart)
    {
        _currentCart = cart;
        OnCartUpdated?.Invoke(cart);
    }
}
