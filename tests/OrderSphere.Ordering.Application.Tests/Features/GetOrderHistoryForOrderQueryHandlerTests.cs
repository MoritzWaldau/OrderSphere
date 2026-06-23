using OrderSphere.Ordering.Application.Features.OrderHistory;

namespace OrderSphere.Ordering.Application.Tests.Features;

public sealed class GetOrderHistoryForOrderQueryHandlerTests
{
    private static GetOrderHistoryForOrderQueryHandler CreateHandler(IOrderingDbContext ctx) =>
        new(ctx, Substitute.For<ILogger<GetOrderHistoryForOrderQueryHandler>>());

    [Fact]
    public async Task Handle_NoEntries_ReturnsEmptyList()
    {
        var history = new List<OrderHistoryEntry>().BuildMockDbSet();
        var ctx = Substitute.For<IOrderingDbContext>();
        ctx.OrderHistory.Returns(history);

        var result = await CreateHandler(ctx).Handle(new(Guid.NewGuid()), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ReturnsOnlyMatchingOrder_OrderedByOccurredAtAscending()
    {
        var orderId = Guid.NewGuid();
        var otherOrderId = Guid.NewGuid();
        var baseTime = new DateTime(2026, 6, 22, 12, 0, 0, DateTimeKind.Utc);

        var entries = new List<OrderHistoryEntry>
        {
            OrderHistoryEntry.Record(orderId, Guid.NewGuid(), "a@x.de", "Confirmed", "Shipped", baseTime.AddMinutes(2)),
            OrderHistoryEntry.Record(orderId, Guid.NewGuid(), "a@x.de", "Pending", "Confirmed", baseTime),
            OrderHistoryEntry.Record(otherOrderId, Guid.NewGuid(), "b@x.de", "Pending", "Confirmed", baseTime.AddMinutes(1)),
        }.BuildMockDbSet();

        var ctx = Substitute.For<IOrderingDbContext>();
        ctx.OrderHistory.Returns(entries);

        var result = await CreateHandler(ctx).Handle(new(orderId), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().HaveCount(2);
        result.Value.Should().OnlyContain(e => e.OrderId == orderId);
        result.Value.Select(e => e.NewStatus).Should().ContainInOrder("Confirmed", "Shipped");
    }
}
