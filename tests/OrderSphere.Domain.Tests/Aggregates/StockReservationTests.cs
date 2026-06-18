using FluentAssertions;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Catalog.Domain.Entities;
using OrderSphere.Catalog.Domain.Enums;
using Xunit;

namespace OrderSphere.Domain.Tests.Aggregates;

public sealed class StockReservationTests
{
    private static StockReservation Create(DateTime expiresAt)
        => new(Guid.NewGuid(), ProductId.New(), 2, expiresAt);

    [Fact]
    public void New_reservation_is_active_before_expiry()
    {
        var now = DateTime.UtcNow;
        var reservation = Create(now.AddMinutes(30));

        reservation.Status.Should().Be(ReservationStatus.Active);
        reservation.IsActive(now).Should().BeTrue();
    }

    [Fact]
    public void IsActive_is_false_after_expiry()
    {
        var now = DateTime.UtcNow;
        var reservation = Create(now.AddMinutes(-1));

        reservation.IsActive(now).Should().BeFalse("the TTL has elapsed");
    }

    [Fact]
    public void Confirm_marks_confirmed_and_no_longer_active()
    {
        var now = DateTime.UtcNow;
        var reservation = Create(now.AddMinutes(30));

        reservation.Confirm();

        reservation.Status.Should().Be(ReservationStatus.Confirmed);
        reservation.IsActive(now).Should().BeFalse("a confirmed hold no longer reserves availability");
    }

    [Fact]
    public void Release_marks_released_and_no_longer_active()
    {
        var now = DateTime.UtcNow;
        var reservation = Create(now.AddMinutes(30));

        reservation.Release();

        reservation.Status.Should().Be(ReservationStatus.Released);
        reservation.IsActive(now).Should().BeFalse();
    }
}
