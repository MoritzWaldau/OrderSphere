using OrderSphere.Ordering.Application.Features.OrderHistory.Admin;

namespace OrderSphere.Ordering.Application.Tests.Features;

public sealed class GetOrderHistoryFeedQueryHandlerTests
{
    private static GetOrderHistoryFeedQueryHandler CreateHandler(IOrderingDbContext ctx) =>
        new(ctx, Substitute.For<ILogger<GetOrderHistoryFeedQueryHandler>>());

    private static IOrderingDbContext ContextWith(IEnumerable<OrderHistoryEntry> entries)
    {
        var history = entries.ToList().BuildMockDbSet();
        var ctx = Substitute.For<IOrderingDbContext>();
        ctx.OrderHistory.Returns(history);
        return ctx;
    }

    private static List<OrderHistoryEntry> Sequence(int count, string email = "a@x.de")
    {
        var baseTime = new DateTime(2026, 6, 22, 12, 0, 0, DateTimeKind.Utc);
        return Enumerable.Range(0, count)
            .Select(i => OrderHistoryEntry.Record(
                Guid.NewGuid(), Guid.NewGuid(), email, "Pending", "Confirmed", baseTime.AddMinutes(i)))
            .ToList();
    }

    [Fact]
    public async Task Handle_ReturnsNewestFirst_WithCorrectPagingMetadata()
    {
        var entries = Sequence(5);
        var result = await CreateHandler(ContextWith(entries)).Handle(new(Page: 1, PageSize: 2), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(5);
        result.Value.Page.Should().Be(1);
        result.Value.PageSize.Should().Be(2);
        result.Value.TotalPages.Should().Be(3);
        result.Value.Items.Should().HaveCount(2);
        // Newest first: the last two minutes (offset 4 then 3).
        result.Value.Items.Should().BeInDescendingOrder(e => e.OccurredAt);
    }

    [Fact]
    public async Task Handle_SecondPage_SkipsFirstPage()
    {
        var entries = Sequence(5);
        var result = await CreateHandler(ContextWith(entries)).Handle(new(Page: 2, PageSize: 2), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().HaveCount(2);
        result.Value.Page.Should().Be(2);
    }

    [Theory]
    [InlineData(0, 10)]   // page below 1 is clamped to 1
    [InlineData(-3, 10)]
    public async Task Handle_ClampsInvalidPageToOne(int page, int pageSize)
    {
        var result = await CreateHandler(ContextWith(Sequence(3))).Handle(new(page, pageSize), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Page.Should().Be(1);
    }

    [Theory]
    [InlineData(0)]       // below 1 → default
    [InlineData(1000)]    // above max → default
    public async Task Handle_ClampsInvalidPageSizeToDefault(int pageSize)
    {
        var result = await CreateHandler(ContextWith(Sequence(3))).Handle(new(Page: 1, PageSize: pageSize), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.PageSize.Should().Be(20);
    }

    [Fact]
    public async Task Handle_FiltersByCustomerEmail()
    {
        var mine = Sequence(2, "mine@x.de");
        var others = Sequence(3, "other@x.de");
        var ctx = ContextWith(mine.Concat(others));

        var result = await CreateHandler(ctx).Handle(new(Page: 1, PageSize: 20, CustomerEmail: "mine@x.de"), default);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalCount.Should().Be(2);
        result.Value.Items.Should().OnlyContain(e => e.CustomerEmail == "mine@x.de");
    }
}
