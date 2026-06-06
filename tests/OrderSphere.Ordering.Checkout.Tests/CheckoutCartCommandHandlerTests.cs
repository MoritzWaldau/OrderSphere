using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.Ordering.Application.Abstractions;
using OrderSphere.Ordering.Application.Features.Checkout;
using OrderSphere.Ordering.Domain.Enums;
using OrderSphere.Ordering.Domain.Errors;
using OrderSphere.Ordering.Domain.Events;
using OrderSphere.Ordering.Domain.ValueObjects;
using Xunit;
using TypedCustomerId = OrderSphere.BuildingBlocks.StronglyTypedIds.CustomerId;

namespace OrderSphere.Ordering.Checkout.Tests;

/// <summary>
/// Unit tests for <see cref="CheckoutCartCommandHandler"/>.
///
/// All external dependencies are substituted. Tests verify:
/// - Correct result codes for each failure path.
/// - Compensating stock restores are called for the right items in the right scenarios.
/// - Cart-clear failure is treated as non-fatal after a successful publish.
/// </summary>
public sealed class CheckoutCartCommandHandlerTests
{
    // ── Stub error (contents irrelevant — handler never inspects the upstream error) ──
    private static readonly Error AnyError = new("stub.error", "stub");

    // ── Fixed test data ───────────────────────────────────────────────────────

    private static readonly Guid CustomerId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid ProductId1 = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ProductId2 = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static readonly Address ShippingAddress = new("Max", "Muster", "Hauptstr. 1", "Berlin", "10115", "DE");

    private static readonly Guid IdempotencyKey = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");

    private static readonly CheckoutCartCommand Command = new(
        CustomerId: TypedCustomerId.From(CustomerId),
        CustomerEmail: "max@example.com",
        CustomerName: "Max Muster",
        ShippingAddress: ShippingAddress,
        PaymentMethod: PaymentMethod.CreditCard,
        IdempotencyKey: IdempotencyKey);

    private static BasketCartInfo CartWithItems(params (Guid productId, int qty)[] items) =>
        new(CustomerId, items.Select(i => new BasketCartItemInfo(i.productId, i.qty)).ToList());

    private static CatalogProductInfo Product(Guid id, int stock = 10) =>
        new(id, $"Product-{id:N}", 19.99m, stock, true);

    // ── SUT factory ──────────────────────────────────────────────────────────

    private readonly ICatalogClient _catalog = Substitute.For<ICatalogClient>();
    private readonly IBasketClient _basket = Substitute.For<IBasketClient>();
    private readonly IOrderingServiceBusPublisher _bus = Substitute.For<IOrderingServiceBusPublisher>();
    private readonly InMemoryCheckoutIdempotencyStore _idempotency = new();
    private readonly ILogger<CheckoutCartCommandHandler> _logger =
        Substitute.For<ILogger<CheckoutCartCommandHandler>>();

    private CheckoutCartCommandHandler CreateHandler() =>
        new(_catalog, _basket, _bus, _idempotency, _logger);

    // ── Cart retrieval failures ───────────────────────────────────────────────

    [Fact]
    public async Task CartNotFound_ReturnsCartNotFoundError()
    {
        _basket.GetCartAsync(CustomerId, Arg.Any<CancellationToken>())
               .Returns(Result<BasketCartInfo>.Failure(AnyError));

        var result = await CreateHandler().Handle(Command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CheckoutCartErrors.CartNotFoundError);
        await _catalog.DidNotReceive().GetProductByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task EmptyCart_ReturnsEmptyCartError()
    {
        _basket.GetCartAsync(CustomerId, Arg.Any<CancellationToken>())
               .Returns(Result<BasketCartInfo>.Success(CartWithItems()));

        var result = await CreateHandler().Handle(Command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CheckoutCartErrors.EmptyCartError);
        await _catalog.DidNotReceive().GetProductByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    // ── Product resolution phase (read-only, no stock changes yet) ───────────

    [Fact]
    public async Task ProductNotFound_ReturnsProductNotFoundError_NoDecrementCalled()
    {
        _basket.GetCartAsync(CustomerId, Arg.Any<CancellationToken>())
               .Returns(Result<BasketCartInfo>.Success(
                   CartWithItems((ProductId1, 1), (ProductId2, 2))));

        _catalog.GetProductByIdAsync(ProductId1, Arg.Any<CancellationToken>())
                .Returns(Result<CatalogProductInfo>.Success(Product(ProductId1)));

        _catalog.GetProductByIdAsync(ProductId2, Arg.Any<CancellationToken>())
                .Returns(Result<CatalogProductInfo>.Failure(ProductErrors.ProductNotFoundError));

        var result = await CreateHandler().Handle(Command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProductErrors.ProductNotFoundError);
        await _catalog.DidNotReceive()
                      .DecrementStockAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    // ── Stock decrement phase ─────────────────────────────────────────────────

    [Fact]
    public async Task InsufficientStock_OnFirstItem_ReturnsError_NoCompensationNeeded()
    {
        _basket.GetCartAsync(CustomerId, Arg.Any<CancellationToken>())
               .Returns(Result<BasketCartInfo>.Success(
                   CartWithItems((ProductId1, 5), (ProductId2, 2))));

        _catalog.GetProductByIdAsync(ProductId1, Arg.Any<CancellationToken>())
                .Returns(Result<CatalogProductInfo>.Success(Product(ProductId1)));
        _catalog.GetProductByIdAsync(ProductId2, Arg.Any<CancellationToken>())
                .Returns(Result<CatalogProductInfo>.Success(Product(ProductId2)));

        _catalog.DecrementStockAsync(ProductId1, 5, Arg.Any<CancellationToken>())
                .Returns(Result.Failure(ProductErrors.InsufficientStockError));

        var result = await CreateHandler().Handle(Command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProductErrors.InsufficientStockError);
        await _catalog.DidNotReceive()
                      .RestoreStockAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task InsufficientStock_OnSecondItem_CompensatesFirstDecrement()
    {
        _basket.GetCartAsync(CustomerId, Arg.Any<CancellationToken>())
               .Returns(Result<BasketCartInfo>.Success(
                   CartWithItems((ProductId1, 2), (ProductId2, 5))));

        _catalog.GetProductByIdAsync(ProductId1, Arg.Any<CancellationToken>())
                .Returns(Result<CatalogProductInfo>.Success(Product(ProductId1)));
        _catalog.GetProductByIdAsync(ProductId2, Arg.Any<CancellationToken>())
                .Returns(Result<CatalogProductInfo>.Success(Product(ProductId2)));

        _catalog.DecrementStockAsync(ProductId1, 2, Arg.Any<CancellationToken>())
                .Returns(Result.Success());
        _catalog.DecrementStockAsync(ProductId2, 5, Arg.Any<CancellationToken>())
                .Returns(Result.Failure(ProductErrors.InsufficientStockError));
        _catalog.RestoreStockAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(Result.Success());

        var result = await CreateHandler().Handle(Command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ProductErrors.InsufficientStockError);
        await _catalog.Received(1).RestoreStockAsync(ProductId1, 2, CancellationToken.None);
        await _catalog.DidNotReceive().RestoreStockAsync(ProductId2, Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    // ── Service Bus publish failure ───────────────────────────────────────────

    [Fact]
    public async Task ServiceBusPublishFails_CompensatesAllDecrementedItems()
    {
        _basket.GetCartAsync(CustomerId, Arg.Any<CancellationToken>())
               .Returns(Result<BasketCartInfo>.Success(
                   CartWithItems((ProductId1, 1), (ProductId2, 3))));

        _catalog.GetProductByIdAsync(ProductId1, Arg.Any<CancellationToken>())
                .Returns(Result<CatalogProductInfo>.Success(Product(ProductId1)));
        _catalog.GetProductByIdAsync(ProductId2, Arg.Any<CancellationToken>())
                .Returns(Result<CatalogProductInfo>.Success(Product(ProductId2)));

        _catalog.DecrementStockAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(Result.Success());
        _catalog.RestoreStockAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(Result.Success());

        _bus.PublishCheckoutCartEventAsync(Arg.Any<CheckoutCartEvent>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("Service Bus unavailable"));

        var result = await CreateHandler().Handle(Command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CheckoutCartErrors.UnknownError);
        await _catalog.Received(1).RestoreStockAsync(ProductId1, 1, CancellationToken.None);
        await _catalog.Received(1).RestoreStockAsync(ProductId2, 3, CancellationToken.None);
        await _basket.DidNotReceive().ClearCartItemsAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    // ── Cart-clear failure is non-fatal ───────────────────────────────────────

    [Fact]
    public async Task CartClearFails_ReturnsSuccess_NoCompensation()
    {
        _basket.GetCartAsync(CustomerId, Arg.Any<CancellationToken>())
               .Returns(Result<BasketCartInfo>.Success(
                   CartWithItems((ProductId1, 1))));

        _catalog.GetProductByIdAsync(ProductId1, Arg.Any<CancellationToken>())
                .Returns(Result<CatalogProductInfo>.Success(Product(ProductId1)));

        _catalog.DecrementStockAsync(ProductId1, 1, Arg.Any<CancellationToken>())
                .Returns(Result.Success());

        _bus.PublishCheckoutCartEventAsync(Arg.Any<CheckoutCartEvent>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _basket.ClearCartItemsAsync(CustomerId, Arg.Any<CancellationToken>())
               .Returns(Result.Failure(AnyError));

        var result = await CreateHandler().Handle(Command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue("cart-clear failure is non-fatal after accepted publish");
        await _catalog.DidNotReceive()
                      .RestoreStockAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    // ── Happy path ────────────────────────────────────────────────────────────

    [Fact]
    public async Task HappyPath_TwoItems_ReturnsCorrelationId_AndClearsCart()
    {
        _basket.GetCartAsync(CustomerId, Arg.Any<CancellationToken>())
               .Returns(Result<BasketCartInfo>.Success(
                   CartWithItems((ProductId1, 2), (ProductId2, 1))));

        _catalog.GetProductByIdAsync(ProductId1, Arg.Any<CancellationToken>())
                .Returns(Result<CatalogProductInfo>.Success(Product(ProductId1)));
        _catalog.GetProductByIdAsync(ProductId2, Arg.Any<CancellationToken>())
                .Returns(Result<CatalogProductInfo>.Success(Product(ProductId2)));

        _catalog.DecrementStockAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(Result.Success());

        _bus.PublishCheckoutCartEventAsync(Arg.Any<CheckoutCartEvent>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _basket.ClearCartItemsAsync(CustomerId, Arg.Any<CancellationToken>())
               .Returns(Result.Success());

        var result = await CreateHandler().Handle(Command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty, "a valid CorrelationId must be returned");

        await _catalog.Received(1).DecrementStockAsync(ProductId1, 2, Arg.Any<CancellationToken>());
        await _catalog.Received(1).DecrementStockAsync(ProductId2, 1, Arg.Any<CancellationToken>());
        await _bus.Received(1).PublishCheckoutCartEventAsync(Arg.Any<CheckoutCartEvent>());
        await _basket.Received(1).ClearCartItemsAsync(CustomerId, Arg.Any<CancellationToken>());
        await _catalog.DidNotReceive()
                      .RestoreStockAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    // ── Idempotency guard ─────────────────────────────────────────────────────

    [Fact]
    public async Task DuplicateIdempotencyKey_ReturnsCachedCorrelationId_WithoutReprocessing()
    {
        // Arrange: first call succeeds and populates the cache.
        _basket.GetCartAsync(CustomerId, Arg.Any<CancellationToken>())
               .Returns(Result<BasketCartInfo>.Success(CartWithItems((ProductId1, 1))));
        _catalog.GetProductByIdAsync(ProductId1, Arg.Any<CancellationToken>())
                .Returns(Result<CatalogProductInfo>.Success(Product(ProductId1)));
        _catalog.DecrementStockAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
                .Returns(Result.Success());
        _bus.PublishCheckoutCartEventAsync(Arg.Any<CheckoutCartEvent>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);
        _basket.ClearCartItemsAsync(CustomerId, Arg.Any<CancellationToken>())
               .Returns(Result.Success());

        var handler = CreateHandler();
        var firstResult = await handler.Handle(Command, CancellationToken.None);
        firstResult.IsSuccess.Should().BeTrue();

        // Act: second call with the same IdempotencyKey.
        var secondResult = await handler.Handle(Command, CancellationToken.None);

        // Assert: same CorrelationId, no second decrement or publish.
        secondResult.IsSuccess.Should().BeTrue();
        secondResult.Value.Should().Be(firstResult.Value, "duplicate request must return the same CorrelationId");
        await _catalog.Received(1).DecrementStockAsync(Arg.Any<Guid>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _bus.Received(1).PublishCheckoutCartEventAsync(Arg.Any<CheckoutCartEvent>());
    }
}
