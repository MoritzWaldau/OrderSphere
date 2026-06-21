using System.Text.Json;
using Azure.Messaging.ServiceBus;
using FluentAssertions;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OrderSphere.BuildingBlocks.Contracts.Events;
using OrderSphere.BuildingBlocks.EventBus.Inbox;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Ordering.Application.Abstractions;
using OrderSphere.Ordering.Domain.Enums;
using OrderSphere.Ordering.Infrastructure.Persistence;
using OrderSphere.Ordering.Worker.Workers;
using Xunit;
using OrderItemDto = OrderSphere.BuildingBlocks.Contracts.Events.OrderItemDto;
using ShippingAddressDto = OrderSphere.BuildingBlocks.Contracts.Events.ShippingAddressDto;

namespace OrderSphere.IntegrationTests;

/// <summary>
/// Covers the A1 Scope B deterministic failure path: when a payment succeeded but the reservation
/// confirm cannot complete after bounded retries, the order is cancelled, a refund is requested
/// (<see cref="OrderConfirmationFailedIntegrationEvent"/>), and the saga moves
/// PaymentRequested → CompensationPending → Refunded once the refund result returns.
///
/// Uses SQLite in-process because OrderItem.Price (Money) is a ComplexProperty the EF Core
/// InMemory provider cannot shape.
/// </summary>
public sealed class SagaCompensationTests : IDisposable
{
    private static readonly Guid CustomerGuid = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
    private static readonly Guid ProductGuid = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<OrderingDbContext> _options;

    public SagaCompensationTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        _options = new DbContextOptionsBuilder<OrderingDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var ctx = NewContext();
        ctx.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    private OrderingDbContext NewContext() => new(_options, Substitute.For<IPublisher>());

    private static PaymentResultProcessor NewPaymentResultProcessor()
        => new(
            Substitute.For<ServiceBusClient>(),
            Substitute.For<IServiceScopeFactory>(),
            NullLogger<PaymentResultProcessor>.Instance);

    private static PaymentRefundProcessor NewRefundProcessor()
        => new(
            Substitute.For<ServiceBusClient>(),
            Substitute.For<IServiceScopeFactory>(),
            NullLogger<PaymentRefundProcessor>.Instance);

    // Confirm always fails; release succeeds (mirrors a reservation reclaimed by the TTL sweeper).
    private static ICatalogClient FailingConfirmCatalog()
    {
        var catalog = Substitute.For<ICatalogClient>();
        catalog.ConfirmReservationAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Failure(new Error("Catalog.Confirm", "Reservation gone."))));
        catalog.ReleaseReservationAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Result.Success()));
        return catalog;
    }

    private async Task<(Guid OrderId, Guid CorrelationId)> SeedOrderAsync()
    {
        var checkoutEvent = new CheckoutCartIntegrationEvent
        {
            CorrelationId = Guid.NewGuid(),
            CustomerId = CustomerGuid,
            CustomerEmail = "customer@example.com",
            CustomerName = "Max Mustermann",
            ShippingAddress = new ShippingAddressDto("Max", "Mustermann", "Hauptstr. 1", "Berlin", "10115", "DE"),
            PaymentMethod = PaymentMethod.CreditCard.ToString(),
            Items = [new OrderItemDto(ProductGuid, "Widget", 2, 9.99m)]
        };

        await using var ctx = NewContext();
        var result = await new OrderProcessor(
            Substitute.For<ServiceBusClient>(),
            Substitute.For<IServiceScopeFactory>(),
            Substitute.For<IShippingRateProvider>(),
            NullLogger<OrderProcessor>.Instance)
            .ProcessOrderAsync(checkoutEvent, ctx, CancellationToken.None);

        if (!result.IsSuccess) throw new InvalidOperationException($"ProcessOrderAsync failed: {result.ErrorMessage}");

        var orderId = await ctx.Orders
            .Where(o => o.CorrelationId == checkoutEvent.CorrelationId)
            .Select(o => o.Id.Value)
            .SingleAsync();

        return (orderId, checkoutEvent.CorrelationId);
    }

    private static PaymentProcessedIntegrationEvent SucceededEvent(Guid orderId, Guid correlationId) => new()
    {
        OrderId = orderId,
        CorrelationId = correlationId,
        Succeeded = true,
        CustomerEmail = "customer@example.com",
        PaymentMethod = "creditcard"
    };

    [Fact]
    public async Task Confirm_failure_below_attempt_budget_throws_and_leaves_saga_pending()
    {
        var (orderId, correlationId) = await SeedOrderAsync();
        var evt = SucceededEvent(orderId, correlationId);

        var inbox = Substitute.For<IInboxStore>();
        inbox.HasBeenProcessedAsync(evt.Id, Arg.Any<CancellationToken>()).Returns(false);

        await using var ctx = NewContext();
        var act = async () => await NewPaymentResultProcessor()
            .ProcessPaymentResultAsync(evt, ctx, inbox, FailingConfirmCatalog(), deliveryCount: 1, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();

        await using var verify = NewContext();
        var saga = await verify.OrderSagas.SingleAsync(s => s.CorrelationId == correlationId);
        saga.State.Should().Be(SagaState.PaymentRequested);
        var order = await verify.Orders.SingleAsync(o => o.Id == OrderId.From(orderId));
        order.Status.Should().Be(OrderStatus.Created);
    }

    [Fact]
    public async Task Confirm_failure_at_attempt_budget_compensates_and_requests_refund()
    {
        var (orderId, correlationId) = await SeedOrderAsync();
        var evt = SucceededEvent(orderId, correlationId);

        var inbox = Substitute.For<IInboxStore>();
        inbox.HasBeenProcessedAsync(evt.Id, Arg.Any<CancellationToken>()).Returns(false);

        await using var ctx = NewContext();
        var outcome = await NewPaymentResultProcessor()
            .ProcessPaymentResultAsync(evt, ctx, inbox, FailingConfirmCatalog(), deliveryCount: 3, CancellationToken.None);
        await ctx.SaveChangesAsync();

        outcome.Should().Be(PaymentResultProcessor.PaymentResultOutcome.Processed);

        await using var verify = NewContext();
        var saga = await verify.OrderSagas.SingleAsync(s => s.CorrelationId == correlationId);
        saga.State.Should().Be(SagaState.CompensationPending);
        saga.CompletedAt.Should().BeNull();
        saga.LastError.Should().NotBeNullOrEmpty();

        var order = await verify.Orders.SingleAsync(o => o.Id == OrderId.From(orderId));
        order.Status.Should().Be(OrderStatus.Cancelled);

        var outbox = await verify.OutboxMessages
            .Where(m => m.Type != nameof(PaymentRequestedIntegrationEvent))
            .ToListAsync();
        outbox.Should().Contain(m => m.Type == nameof(OrderConfirmationFailedIntegrationEvent));
        outbox.Should().NotContain(m => m.Type == nameof(OrderPlacedIntegrationEvent));

        var failedJson = outbox.Single(m => m.Type == nameof(OrderConfirmationFailedIntegrationEvent)).Content;
        var failed = JsonSerializer.Deserialize<OrderConfirmationFailedIntegrationEvent>(failedJson);
        failed!.OrderId.Should().Be(orderId);
        failed.CorrelationId.Should().Be(correlationId);
        failed.Amount.Should().Be(19.98m);
    }

    [Fact]
    public async Task Refund_result_advances_saga_to_terminal_Refunded()
    {
        var (orderId, correlationId) = await SeedOrderAsync();

        // Drive the saga to CompensationPending via the confirm-failure path.
        var paid = SucceededEvent(orderId, correlationId);
        var inbox = Substitute.For<IInboxStore>();
        inbox.HasBeenProcessedAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);

        await using (var ctx = NewContext())
        {
            await NewPaymentResultProcessor()
                .ProcessPaymentResultAsync(paid, ctx, inbox, FailingConfirmCatalog(), deliveryCount: 3, CancellationToken.None);
            await ctx.SaveChangesAsync();
        }

        // Payment reports the refund — saga must terminate at Refunded.
        var refunded = new PaymentRefundedIntegrationEvent
        {
            CorrelationId = correlationId,
            OrderId = orderId,
            TransactionId = "ref-1",
            Reason = "compensation"
        };

        await using (var ctx = NewContext())
        {
            await NewRefundProcessor().ProcessRefundAsync(refunded, ctx, inbox, CancellationToken.None);
            await ctx.SaveChangesAsync();
        }

        await using var verify = NewContext();
        var saga = await verify.OrderSagas.SingleAsync(s => s.CorrelationId == correlationId);
        saga.State.Should().Be(SagaState.Refunded);
        saga.CompletedAt.Should().NotBeNull();
    }
}
