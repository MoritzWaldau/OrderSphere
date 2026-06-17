namespace OrderSphere.Web.Tests.Services;

public sealed class CartStateTests
{
    private static CartDto Cart(params int[] quantities)
        => new(Guid.NewGuid(),
               quantities.Select(q => new CartItemDto(Guid.NewGuid(), "Product", 9.99m, q)).ToList());

    private static IOrderingClient OrderingReturning(params ApiResult<CartDto>[] results)
    {
        var ordering = Substitute.For<IOrderingClient>();
        ordering.GetCartAsync(Arg.Any<CancellationToken>()).Returns(results[0], results[1..]);
        return ordering;
    }

    [Fact]
    public async Task RefreshAsync_Success_UpdatesCartAndItemCount()
    {
        var sut = new CartState(OrderingReturning(ApiResult<CartDto>.Ok(Cart(2, 3))));

        await sut.RefreshAsync();

        sut.Cart.Should().NotBeNull();
        sut.ItemCount.Should().Be(5);
    }

    [Fact]
    public async Task RefreshAsync_Failure_KeepsLastKnownCart()
    {
        var sut = new CartState(OrderingReturning(
            ApiResult<CartDto>.Ok(Cart(1)),
            ApiResult<CartDto>.Fail(ApiError.Network)));

        await sut.RefreshAsync(); // success: 1 item
        await sut.RefreshAsync(); // failure: keep the previous cart

        sut.ItemCount.Should().Be(1);
    }

    [Fact]
    public async Task RefreshAsync_RaisesOnChange()
    {
        var sut = new CartState(OrderingReturning(ApiResult<CartDto>.Ok(Cart(1))));
        var raised = false;
        sut.OnChange += () => raised = true;

        await sut.RefreshAsync();

        raised.Should().BeTrue();
    }

    [Fact]
    public async Task InitializeAsync_FetchesOnlyOnce()
    {
        var ordering = OrderingReturning(ApiResult<CartDto>.Ok(Cart(1)));
        var sut = new CartState(ordering);

        await sut.InitializeAsync();
        await sut.InitializeAsync();

        await ordering.Received(1).GetCartAsync(Arg.Any<CancellationToken>());
    }
}
