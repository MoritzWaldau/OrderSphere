using FluentAssertions;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Ordering.Domain.Entities;
using OrderSphere.Ordering.Domain.Enums;
using OrderSphere.Ordering.Domain.Errors;
using Xunit;

namespace OrderSphere.Domain.Tests.Aggregates;

public sealed class ReturnRequestTests
{
    private static readonly DateTime Now = new(2026, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    private static ReturnRequest NewRequest() =>
        new(
            OrderId.New(),
            CustomerId.New(),
            "Defekt",
            "EUR",
            [new ReturnItem(ProductId.New(), "Widget", 2, 9.99m)],
            Now);

    [Fact]
    public void Constructor_StartsInRequested_WithComputedRefundAmount()
    {
        var request = NewRequest();

        request.Status.Should().Be(ReturnStatus.Requested);
        request.RefundAmount.Should().Be(19.98m);
        request.ResolvedAt.Should().BeNull();
    }

    [Fact]
    public void Approve_FromRequested_TransitionsToApproved()
    {
        var request = NewRequest();

        var result = request.Approve("ok", Now);

        result.IsSuccess.Should().BeTrue();
        request.Status.Should().Be(ReturnStatus.Approved);
        request.Resolution.Should().Be("ok");
        request.ResolvedAt.Should().Be(Now);
    }

    [Fact]
    public void Reject_FromRequested_TransitionsToRejected()
    {
        var request = NewRequest();

        var result = request.Reject("kein Anspruch", Now);

        result.IsSuccess.Should().BeTrue();
        request.Status.Should().Be(ReturnStatus.Rejected);
    }

    [Fact]
    public void Approve_WhenNotRequested_Fails()
    {
        var request = NewRequest();
        request.Reject(null, Now);

        var result = request.Approve(null, Now);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ReturnErrors.InvalidStatusTransition);
    }

    [Fact]
    public void MarkRefunded_FromApproved_TransitionsToRefunded()
    {
        var request = NewRequest();
        request.Approve(null, Now);

        var result = request.MarkRefunded();

        result.IsSuccess.Should().BeTrue();
        request.Status.Should().Be(ReturnStatus.Refunded);
    }

    [Fact]
    public void MarkRefunded_WhenStillRequested_Fails()
    {
        var request = NewRequest();

        var result = request.MarkRefunded();

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(ReturnErrors.InvalidStatusTransition);
    }

    [Fact]
    public void MarkRefunded_IsIdempotent_WhenAlreadyRefunded()
    {
        var request = NewRequest();
        request.Approve(null, Now);
        request.MarkRefunded();

        var result = request.MarkRefunded();

        result.IsSuccess.Should().BeTrue();
        request.Status.Should().Be(ReturnStatus.Refunded);
    }
}
