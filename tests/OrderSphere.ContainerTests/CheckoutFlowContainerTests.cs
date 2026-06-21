using Azure.Messaging.ServiceBus;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using OrderSphere.BuildingBlocks.Contracts.Events;
using OrderSphere.Ordering.Domain.Enums;
using OrderSphere.Ordering.Infrastructure.Persistence;
using Xunit;

namespace OrderSphere.ContainerTests;

/// <summary>
/// First container tier: drives the checkout-to-payment flow over the real Service Bus emulator
/// and asserts the outcome in the real (migrated) Postgres database — exercising what the
/// in-process suite stubs: cross-queue dispatch (orders → payment-requests → payment-results),
/// the outbox/inbox, and the workers consuming end to end.
///
/// A CheckoutCartIntegrationEvent published directly to the 'orders' queue (no upstream stock
/// reservation) follows the happy path: confirm on a missing reservation is a successful no-op,
/// so the saga reaches Confirmed.
/// </summary>
[Trait("Category", "Container")]
[Collection(AspireAppCollection.Name)]
public sealed class CheckoutFlowContainerTests(AspireAppFixture fixture)
{
    private static readonly Guid ProductGuid = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");

    private OrderingDbContext NewOrderingContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<OrderingDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        return new OrderingDbContext(options, Substitute.For<IPublisher>());
    }

    private static CheckoutCartIntegrationEvent NewCheckout(Guid correlationId) => new()
    {
        CorrelationId = correlationId,
        CustomerId = Guid.NewGuid(),
        CustomerEmail = "container-test@example.com",
        CustomerName = "Container Test",
        ShippingAddress = new ShippingAddressDto("Container", "Test", "Hauptstr. 1", "Berlin", "10115", "DE"),
        PaymentMethod = "CreditCard",
        Items = [new OrderItemDto(ProductGuid, "Widget", 2, 9.99m)]
    };

    [Fact]
    public async Task Checkout_event_drives_order_and_saga_to_terminal_state_over_the_real_bus()
    {
        var correlationId = Guid.NewGuid();
        var busConnectionString = await fixture.ConnectionStringAsync("azure-service-bus");
        var orderingConnectionString = await fixture.ConnectionStringAsync("ordering-db");

        // Publish the production checkout contract straight onto the 'orders' queue.
        await using (var client = new ServiceBusClient(busConnectionString))
        await using (var sender = client.CreateSender("orders"))
        {
            await sender.SendMessageAsync(
                new ServiceBusMessage(BinaryData.FromObjectAsJson(NewCheckout(correlationId))));
        }

        // Poll the real database until the saga reaches a terminal state.
        var deadline = DateTime.UtcNow.AddSeconds(120);
        OrderSphere.Ordering.Domain.Entities.OrderSaga? saga = null;
        while (DateTime.UtcNow < deadline)
        {
            await using var context = NewOrderingContext(orderingConnectionString);
            saga = await context.OrderSagas
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.CorrelationId == correlationId);

            if (saga is { State: SagaState.Confirmed or SagaState.Cancelled or SagaState.Refunded })
                break;

            await Task.Delay(1000);
        }

        saga.Should().NotBeNull("the ordering worker should have projected a saga for the checkout");
        saga!.State.Should().Be(SagaState.Confirmed,
            "no reservation existed, so reservation-confirm is a successful no-op and the order is confirmed");

        await using var verify = NewOrderingContext(orderingConnectionString);
        var order = await verify.Orders.AsNoTracking()
            .FirstOrDefaultAsync(o => o.CorrelationId == correlationId);
        order.Should().NotBeNull();
        order!.Status.Should().Be(OrderStatus.Paid,
            "payment captured successfully, so the order is marked Paid");
    }
}
