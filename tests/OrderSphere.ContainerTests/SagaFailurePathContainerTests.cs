using Azure.Messaging.ServiceBus;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using OrderSphere.BuildingBlocks.Contracts.Events;
using OrderSphere.Catalog.Domain.Entities;
using OrderSphere.Catalog.Infrastructure.Persistence;
using OrderSphere.Ordering.Domain.Enums;
using OrderSphere.Ordering.Infrastructure.Persistence;
using Xunit;

namespace OrderSphere.ContainerTests;

/// <summary>
/// Drives the A1 deterministic compensation loop end to end over the real bus and databases:
/// a paid order whose reservation-confirm fails after the bounded retry budget must compensate
/// with a refund, not loop to the dead-letter queue.
///
/// The precondition is seeded directly in the Catalog database: an active StockReservation keyed
/// by the checkout correlation id whose quantity exceeds the product's on-hand stock. When the
/// payment succeeds, PaymentResultProcessor confirms the reservation, which calls
/// Product.RemoveFromStock(reservedQuantity) and fails (Conflict). After MaxConfirmAttempts the
/// worker raises OrderConfirmationFailed → Payment refunds → PaymentRefunded → saga Refunded.
/// </summary>
[Trait("Category", "Container")]
[Collection(AspireAppCollection.Name)]
public sealed class SagaFailurePathContainerTests(AspireAppFixture fixture)
{
    private CatalogDbContext NewCatalogContext(string connectionString)
        => new(new DbContextOptionsBuilder<CatalogDbContext>().UseNpgsql(connectionString).Options,
            Substitute.For<IPublisher>());

    private OrderingDbContext NewOrderingContext(string connectionString)
        => new(new DbContextOptionsBuilder<OrderingDbContext>().UseNpgsql(connectionString).Options,
            Substitute.For<IPublisher>());

    [Fact]
    public async Task Unconfirmable_paid_order_compensates_with_a_refund_and_the_saga_reaches_refunded()
    {
        var correlationId = Guid.NewGuid();
        var busConnectionString = await fixture.ConnectionStringAsync("azure-service-bus");
        var catalogConnectionString = await fixture.ConnectionStringAsync("catalog-db");
        var orderingConnectionString = await fixture.ConnectionStringAsync("ordering-db");

        // Take any seeded product and reserve more than its entire on-hand stock against this
        // correlation id. The reservation is active and unexpired, so confirm will pick it up and
        // RemoveFromStock will fail deterministically — independent of the product's actual stock.
        Product product;
        var productDeadline = DateTime.UtcNow.AddSeconds(60);
        while (true)
        {
            await using var catalog = NewCatalogContext(catalogConnectionString);
            var seeded = await catalog.Products.AsNoTracking().FirstOrDefaultAsync();
            if (seeded is not null)
            {
                product = seeded;
                break;
            }

            DateTime.UtcNow.Should().BeBefore(productDeadline, "the catalog seeder should populate products at startup");
            await Task.Delay(1000);
        }

        await using (var catalog = NewCatalogContext(catalogConnectionString))
        {
            catalog.StockReservations.Add(new StockReservation(
                correlationId, product.Id, product.Stock + 1000, DateTime.UtcNow.AddMinutes(30)));
            await catalog.SaveChangesAsync();
        }

        // Publish a checkout for the same correlation id. The order item quantity is independent of
        // the (deliberately oversized) reservation that drives the confirm failure.
        var checkout = new CheckoutCartIntegrationEvent
        {
            CorrelationId = correlationId,
            CustomerId = Guid.NewGuid(),
            CustomerEmail = "saga-failure@example.com",
            CustomerName = "Saga Failure",
            ShippingAddress = new ShippingAddressDto("Saga", "Failure", "Hauptstr. 1", "Berlin", "10115", "DE"),
            PaymentMethod = "CreditCard",
            Items = [new OrderItemDto(product.Id.Value, product.Name, 1, 9.99m)]
        };

        await using (var client = new ServiceBusClient(busConnectionString))
        await using (var sender = client.CreateSender("orders"))
        {
            await sender.SendMessageAsync(
                new ServiceBusMessage(BinaryData.FromObjectAsJson(checkout)));
        }

        // The loop spans three queues (orders → payment-requests/results → order-confirmation-failed
        // → payment-refunds), bounded confirm retries, and the refund round-trip — allow generous time.
        var deadline = DateTime.UtcNow.AddSeconds(240);
        OrderSphere.Ordering.Domain.Entities.OrderSaga? saga = null;
        while (DateTime.UtcNow < deadline)
        {
            await using var context = NewOrderingContext(orderingConnectionString);
            saga = await context.OrderSagas.AsNoTracking()
                .FirstOrDefaultAsync(s => s.CorrelationId == correlationId);

            if (saga is { State: SagaState.Refunded or SagaState.Confirmed or SagaState.Cancelled })
                break;

            await Task.Delay(2000);
        }

        saga.Should().NotBeNull("the ordering worker should have projected a saga for the checkout");
        saga!.State.Should().Be(SagaState.Refunded,
            "the reservation confirm fails after the retry budget, so the order is compensated by a refund");

        await using var verify = NewOrderingContext(orderingConnectionString);
        var order = await verify.Orders.AsNoTracking()
            .FirstOrDefaultAsync(o => o.CorrelationId == correlationId);
        order.Should().NotBeNull();
        order!.Status.Should().Be(OrderStatus.Cancelled,
            "an unconfirmable order is cancelled before its payment is refunded");
    }
}
