using OrderSphere.Ordering.Application.Features.Saga;

namespace OrderSphere.Ordering.Application.Tests.Features;

public sealed class GetSagaByCorrelationIdQueryHandlerTests
{
    private static GetSagaByCorrelationIdQueryHandler CreateHandler(IOrderingDbContext ctx) =>
        new(ctx, Substitute.For<ILogger<GetSagaByCorrelationIdQueryHandler>>());

    [Fact]
    public async Task Handle_SagaNotFound_ReturnsSuccessWithNullValue()
    {
        var sagas = new List<OrderSaga>().BuildMockDbSet();
        var ctx = Substitute.For<IOrderingDbContext>();
        ctx.OrderSagas.Returns(sagas);

        var result = await CreateHandler(ctx).Handle(new(Guid.NewGuid()), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeNull();
    }

    [Fact]
    public async Task Handle_SagaExists_ReturnsMappedDto()
    {
        var correlationId = Guid.NewGuid();
        var orderId = Guid.NewGuid();
        var saga = OrderSaga.Start(correlationId, orderId);
        saga.MarkPaymentRequested();
        saga.MarkConfirmed();

        var sagas = new List<OrderSaga> { saga }.BuildMockDbSet();
        var ctx = Substitute.For<IOrderingDbContext>();
        ctx.OrderSagas.Returns(sagas);

        var result = await CreateHandler(ctx).Handle(new(correlationId), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.CorrelationId.Should().Be(correlationId);
        result.Value.OrderId.Should().Be(orderId);
        result.Value.State.Should().Be(nameof(SagaState.Confirmed));
        result.Value.PaymentRequestedAt.Should().NotBeNull();
        result.Value.CompletedAt.Should().NotBeNull();
    }
}
