using OrderSphere.Web.Models;

namespace OrderSphere.Web.Services;

/// <summary>
/// Scoped service that caches the cart in WASM memory and notifies subscribers on change.
/// Cart is loaded once per session; callers can force a refresh via RefreshAsync.
/// </summary>
public sealed class CartState
{
    private readonly IOrderingClient _ordering;

    private CartDto? _cart;
    private Guid? _customerId;

    public event Action? OnChange;

    public CartDto? Cart => _cart;
    public int ItemCount => _cart?.Items.Sum(i => i.Quantity) ?? 0;

    public CartState(IOrderingClient ordering)
    {
        _ordering = ordering;
    }

    public async Task InitializeAsync(Guid customerId, CancellationToken ct = default)
    {
        if (_customerId == customerId && _cart is not null) return;
        _customerId = customerId;
        await RefreshAsync(ct);
    }

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        if (_customerId is null) return;
        _cart = await _ordering.GetCartAsync(_customerId.Value, ct);
        NotifyChanged();
    }

    public void NotifyChanged() => OnChange?.Invoke();
}
