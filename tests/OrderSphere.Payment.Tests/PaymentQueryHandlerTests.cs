using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Payment.Application.Features.Payments;
using OrderSphere.Payment.Domain.Entities;
using OrderSphere.Payment.Domain.Errors;
using OrderSphere.Payment.Infrastructure.Persistence;
using Xunit;

namespace OrderSphere.Payment.Tests;

public sealed class PaymentQueryHandlerTests
{
    private static PaymentDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PaymentDbContext(options, Substitute.For<IPublisher>());
    }

    private static PaymentRecord NewRecord(Guid orderId, Action<PaymentRecord>? mutate = null)
    {
        var record = new PaymentRecord(
            OrderId.From(orderId), 49.99m, "EUR", "creditcard", "customer@example.com", Guid.NewGuid());
        mutate?.Invoke(record);
        return record;
    }

    [Fact]
    public async Task GetById_ExistingPayment_ReturnsDto()
    {
        await using var ctx = NewContext();
        var orderId = Guid.NewGuid();
        var record = NewRecord(orderId, r => r.MarkCaptured("tx-1"));
        ctx.Payments.Add(record);
        await ctx.SaveChangesAsync();

        var result = await new GetPaymentByIdQueryHandler(ctx).Handle(new(record.Id.Value), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.OrderId.Should().Be(orderId);
        result.Value.Status.Should().Be("Captured");
        result.Value.TransactionId.Should().Be("tx-1");
    }

    [Fact]
    public async Task GetById_UnknownPayment_ReturnsNotFound()
    {
        await using var ctx = NewContext();

        var result = await new GetPaymentByIdQueryHandler(ctx).Handle(new(Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PaymentErrors.PaymentNotFound);
    }

    [Fact]
    public async Task GetByOrderId_ExistingPayment_ReturnsDto()
    {
        await using var ctx = NewContext();
        var orderId = Guid.NewGuid();
        ctx.Payments.Add(NewRecord(orderId));
        await ctx.SaveChangesAsync();

        var result = await new GetPaymentByOrderIdQueryHandler(ctx).Handle(new(orderId), default);

        result.IsSuccess.Should().BeTrue();
        result.Value.OrderId.Should().Be(orderId);
        result.Value.Status.Should().Be("Pending");
    }

    [Fact]
    public async Task GetByOrderId_UnknownOrder_ReturnsNotFound()
    {
        await using var ctx = NewContext();

        var result = await new GetPaymentByOrderIdQueryHandler(ctx).Handle(new(Guid.NewGuid()), default);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(PaymentErrors.PaymentNotFound);
    }
}
