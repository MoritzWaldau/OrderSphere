using OrderSphere.Web.Models;

namespace OrderSphere.Web.Services;

/// <summary>
/// Scoped service that caches the cart in WASM memory and notifies subscribers on change.
/// Cart is loaded once per session; callers can force a refresh via <see cref="RefreshAsync"/>.
/// Customer identity is resolved server-side from the JWT token; no client-side ID is needed.
/// </summary>
public sealed class CartState
{
    private readonly IOrderingClient _ordering;

    private CartDto? _cart;
    private bool _initialized;

    public event Action? OnChange;

    public CartDto? Cart => _cart;
    public int ItemCount => _cart?.Items.Sum(i => i.Quantity) ?? 0;

    public CartState(IOrderingClient ordering)
    {
        _ordering = ordering;
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        if (_initialized) return;
        _initialized = true;
        await RefreshAsync(ct);
    }

    public async Task RefreshAsync(CancellationToken ct = default)
    {
        // Keep the last known cart on a transient failure rather than wiping the UI.
        var result = await _ordering.GetCartAsync(ct);
        if (result.IsSuccess)
            _cart = result.Value;
        NotifyChanged();
    }

    public void NotifyChanged() => OnChange?.Invoke();
}
