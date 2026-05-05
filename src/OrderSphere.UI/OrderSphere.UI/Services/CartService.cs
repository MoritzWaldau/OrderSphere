using MediatR;
using OrderSphere.Application.Features.Cart.GetCart;
using OrderSphere.Application.Models;
using OrderSphere.UI.Models.Cart.Events;
using OrderSphere.UI.Services.Auth;

namespace OrderSphere.UI.Services;

public interface ICartService
{
    event Action<CartChangedEventArgs>? OnCartUpdated;

    Task<CartDto?> GetCartAsync();
    Task RefreshCartAsync();
    void UpdateCart(Guid customerId, CartDto cart);
    void ClearCart();
    void ClearCart(Guid customerId);
    Task ClearCurrentUserCartAsync();
}

public sealed class CartService : ICartService, IDisposable
{
    private readonly ISender _sender;
    private readonly ICurrentUserService _currentUserService;
    private readonly Dictionary<Guid, CartDto?> _cartCache = new();

    public event Action<CartChangedEventArgs>? OnCartUpdated;

    public CartService(
        ISender sender,
        ICurrentUserService currentUserService)
    {
        _sender = sender;
        _currentUserService = currentUserService;
        _currentUserService.OnCurrentUserChanged += HandleCurrentUserChanged;
    }

    public async Task<CartDto?> GetCartAsync()
    {
        var customerId = await _currentUserService.GetCustomerIdAsync();

        if (customerId is null)
            return null;

        if (_cartCache.TryGetValue(customerId.Value, out var cachedCart))
            return cachedCart;

        await RefreshCartAsync();

        return _cartCache.TryGetValue(customerId.Value, out var loadedCart)
            ? loadedCart
            : null;
    }

    public async Task RefreshCartAsync()
    {
        var customerId = await _currentUserService.GetCustomerIdAsync();

        if (customerId is null)
        {
            ClearCart();
            return;
        }

        try
        {
            var result = await _sender.Send(new GetCartQuery(customerId.Value));

            if (!result.IsSuccess)
                return;

            UpdateCart(customerId.Value, result.Value);
        }
        catch
        {
        }
    }

    public void UpdateCart(Guid customerId, CartDto cart)
    {
        if (customerId == Guid.Empty)
            return;

        _cartCache[customerId] = cart;

        OnCartUpdated?.Invoke(new CartChangedEventArgs
        {
            CustomerId = customerId,
            Cart = cart
        });
    }

    public void ClearCart()
    {
        foreach (var customerId in _cartCache.Keys.ToList())
        {
            OnCartUpdated?.Invoke(new CartChangedEventArgs
            {
                CustomerId = customerId,
                Cart = null
            });
        }

        _cartCache.Clear();
    }

    public void ClearCart(Guid customerId)
    {
        if (customerId == Guid.Empty)
            return;

        _cartCache.Remove(customerId);

        OnCartUpdated?.Invoke(new CartChangedEventArgs
        {
            CustomerId = customerId,
            Cart = null
        });
    }

    public async Task ClearCurrentUserCartAsync()
    {
        var customerId = await _currentUserService.GetCustomerIdAsync();

        if (customerId is not null)
        {
            ClearCart(customerId.Value);
        }
    }

    private void HandleCurrentUserChanged()
    {
        ClearCart();
    }

    public void Dispose()
    {
        _currentUserService.OnCurrentUserChanged -= HandleCurrentUserChanged;
    }
}
