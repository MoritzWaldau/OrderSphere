using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using OrderSphere.Application.Features.Cart.GetCart;
using OrderSphere.Application.Models;
using OrderSphere.UI.Models.Cart.Events;
using OrderSphere.UI.Services.Auth;
using StackExchange.Redis;
using System.Text.Json;

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

public sealed class CartService : ICartService, IAsyncDisposable
{
    private static readonly RedisChannel CartChangedChannel = RedisChannel.Literal("cart-changed");
    private static readonly DistributedCacheEntryOptions CacheOptions = new()
    {
        SlidingExpiration = TimeSpan.FromHours(1),
        AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(30)
    };

    private readonly ISender _sender;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDistributedCache _cache;
    private readonly IConnectionMultiplexer _redis;
    private readonly ILogger<CartService> _logger;
    private readonly ChannelMessageQueue _subscription;
    private readonly Guid _instanceId = Guid.NewGuid();

    private Guid? _lastKnownCustomerId;

    public event Action<CartChangedEventArgs>? OnCartUpdated;

    public CartService(
        ISender sender,
        ICurrentUserService currentUserService,
        IDistributedCache cache,
        IConnectionMultiplexer redis,
        ILogger<CartService> logger)
    {
        _sender = sender;
        _currentUserService = currentUserService;
        _cache = cache;
        _redis = redis;
        _logger = logger;

        _currentUserService.OnCurrentUserChanged += HandleCurrentUserChanged;

        _subscription = _redis.GetSubscriber().Subscribe(CartChangedChannel);
        _subscription.OnMessage(HandleCartChangedMessageAsync);
    }

    public async Task<CartDto?> GetCartAsync()
    {
        var customerId = await _currentUserService.GetCustomerIdAsync();
        if (customerId is null)
            return null;

        _lastKnownCustomerId = customerId;
        return await ReadFromCacheOrDbAsync(customerId.Value);
    }

    public async Task RefreshCartAsync()
    {
        var customerId = await _currentUserService.GetCustomerIdAsync();
        if (customerId is null)
        {
            ClearCart();
            return;
        }

        CartDto? cart;
        try
        {
            var result = await _sender.Send(new GetCartQuery(customerId.Value));
            cart = result.IsSuccess ? result.Value : null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RefreshCartAsync: failed to load cart for customer {CustomerId}", customerId);
            return;
        }

        _lastKnownCustomerId = customerId;

        // Fire UI event immediately so the local circuit reflects the new state,
        // independent of cache/pub-sub availability.
        OnCartUpdated?.Invoke(new CartChangedEventArgs
        {
            CustomerId = customerId.Value,
            Cart = cart
        });

        // Update distributed cache + notify other circuits in the background.
        _ = WriteAndPublishAsync(customerId.Value, cart);
    }

    public void UpdateCart(Guid customerId, CartDto cart)
    {
        if (customerId == Guid.Empty)
            return;

        _lastKnownCustomerId = customerId;

        OnCartUpdated?.Invoke(new CartChangedEventArgs
        {
            CustomerId = customerId,
            Cart = cart
        });

        _ = WriteAndPublishAsync(customerId, cart);
    }

    public void ClearCart()
    {
        if (_lastKnownCustomerId is null)
            return;

        var customerId = _lastKnownCustomerId.Value;
        _lastKnownCustomerId = null;

        OnCartUpdated?.Invoke(new CartChangedEventArgs
        {
            CustomerId = customerId,
            Cart = null
        });
    }

    public void ClearCart(Guid customerId)
    {
        if (customerId == Guid.Empty)
            return;

        OnCartUpdated?.Invoke(new CartChangedEventArgs
        {
            CustomerId = customerId,
            Cart = null
        });

        _ = RemoveAndPublishAsync(customerId);
    }

    public async Task ClearCurrentUserCartAsync()
    {
        var customerId = await _currentUserService.GetCustomerIdAsync();
        if (customerId is not null)
            ClearCart(customerId.Value);
    }

    private async Task<CartDto?> ReadFromCacheOrDbAsync(Guid customerId)
    {
        try
        {
            var cached = await _cache.GetStringAsync(CartKey(customerId));
            if (cached is not null)
            {
                return cached == NullMarker
                    ? null
                    : JsonSerializer.Deserialize<CartDto>(cached);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ReadFromCacheOrDbAsync: distributed cache read failed for customer {CustomerId}, falling back to DB", customerId);
        }

        try
        {
            var result = await _sender.Send(new GetCartQuery(customerId));
            if (!result.IsSuccess)
                return null;

            _ = WriteToCacheSafelyAsync(customerId, result.Value);
            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ReadFromCacheOrDbAsync: DB load failed for customer {CustomerId}", customerId);
            return null;
        }
    }

    private async Task WriteAndPublishAsync(Guid customerId, CartDto? cart)
    {
        await WriteToCacheSafelyAsync(customerId, cart);

        try
        {
            var payload = $"{_instanceId:N}:{customerId:N}";
            await _redis.GetSubscriber().PublishAsync(CartChangedChannel, payload);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WriteAndPublishAsync: failed to publish cart change for customer {CustomerId}", customerId);
        }
    }

    private async Task RemoveAndPublishAsync(Guid customerId)
    {
        try
        {
            await _cache.RemoveAsync(CartKey(customerId));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RemoveAndPublishAsync: failed to remove cache entry for customer {CustomerId}", customerId);
        }

        try
        {
            var payload = $"{_instanceId:N}:{customerId:N}";
            await _redis.GetSubscriber().PublishAsync(CartChangedChannel, payload);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RemoveAndPublishAsync: failed to publish cart change for customer {CustomerId}", customerId);
        }
    }

    private async Task WriteToCacheSafelyAsync(Guid customerId, CartDto? cart)
    {
        try
        {
            var json = cart is null ? NullMarker : JsonSerializer.Serialize(cart);
            await _cache.SetStringAsync(CartKey(customerId), json, CacheOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "WriteToCacheSafelyAsync: failed to write cart cache for customer {CustomerId}", customerId);
        }
    }

    private async Task HandleCartChangedMessageAsync(ChannelMessage message)
    {
        var raw = message.Message.ToString();
        if (string.IsNullOrEmpty(raw))
            return;

        // Payload format: "{publisherInstanceId:N}:{customerId:N}"
        var separatorIndex = raw.IndexOf(':');
        if (separatorIndex <= 0)
            return;

        var publisherSegment = raw[..separatorIndex];
        var customerSegment = raw[(separatorIndex + 1)..];

        if (!Guid.TryParseExact(publisherSegment, "N", out var publisherInstanceId))
            return;

        // Skip messages we published ourselves; the local circuit already raised the event.
        if (publisherInstanceId == _instanceId)
            return;

        if (!Guid.TryParseExact(customerSegment, "N", out var customerId))
            return;

        if (_lastKnownCustomerId is null || _lastKnownCustomerId.Value != customerId)
            return;

        try
        {
            var cart = await ReadFromCacheOrDbAsync(customerId);
            OnCartUpdated?.Invoke(new CartChangedEventArgs
            {
                CustomerId = customerId,
                Cart = cart
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HandleCartChangedMessageAsync: failed to dispatch cart change for customer {CustomerId}", customerId);
        }
    }

    private void HandleCurrentUserChanged()
    {
        ClearCart();
    }

    private static string CartKey(Guid customerId) => $"cart:{customerId}";

    private const string NullMarker = "null";

    public async ValueTask DisposeAsync()
    {
        _currentUserService.OnCurrentUserChanged -= HandleCurrentUserChanged;
        try
        {
            await _subscription.UnsubscribeAsync();
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "DisposeAsync: error while unsubscribing from {Channel}", CartChangedChannel);
        }
    }
}
