using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.BuildingBlocks.ValueObjects;
using OrderSphere.Ordering.Application.Abstractions;
using OrderSphere.Ordering.Domain.Entities;
using OrderSphere.Ordering.Domain.Enums;
using OrderSphere.Ordering.Domain.OrderEvents;
using OrderSphere.Ordering.Domain.ReadModels;
using OrderSphere.Ordering.Domain.ValueObjects;
using OrderSphere.Ordering.Infrastructure.Persistence;

namespace OrderSphere.Ordering.Infrastructure.EventSourcing;

/// <summary>
/// Event store for the <see cref="Order"/> aggregate backed by the ordering DbContext. Append
/// stages event rows and the synchronous read projection into the change tracker without saving —
/// the caller's <c>SaveChanges</c> commits the stream, the projection, and any outbox/inbox rows
/// in one transaction.
/// </summary>
public sealed class OrderEventStore(OrderingDbContext context) : IOrderEventStore
{
    public async Task<Order?> LoadAsync(OrderId id, CancellationToken ct = default)
    {
        var records = await context.Set<OrderEventRecord>()
            .AsNoTracking()
            .Where(e => e.StreamId == id.Value)
            .OrderBy(e => e.Version)
            .ToListAsync(ct);

        if (records.Count == 0)
            return null;

        var events = records.Select(r => OrderEventSerializer.Deserialize(r.EventType, r.Payload));
        return Order.Rehydrate(id, events);
    }

    public async Task AppendAsync(Order order, CancellationToken ct = default)
    {
        var newEvents = order.UncommittedEvents;
        if (newEvents.Count == 0)
            return;

        // Committed length is the aggregate's version minus what we are about to append. New rows
        // continue the 1-based sequence; a concurrent append to the same stream collides here.
        var baseVersion = order.Version - newEvents.Count;
        for (var i = 0; i < newEvents.Count; i++)
        {
            var @event = newEvents[i];
            context.Set<OrderEventRecord>().Add(new OrderEventRecord
            {
                StreamId = order.Id.Value,
                Version = baseVersion + i + 1,
                EventType = OrderEventSerializer.TypeName(@event),
                Payload = OrderEventSerializer.Serialize(@event),
                OccurredAt = @event.OccurredAt
            });
        }

        await ProjectAsync(order.Id, newEvents, ct);
        order.MarkEventsCommitted();
    }

    /// <summary>
    /// Folds the new events into the read row. A stream that starts with <see cref="OrderCreated"/>
    /// inserts a fresh row; otherwise the tracked row is loaded (its owned status timeline comes
    /// with it) so subsequent transitions append exactly one history entry each.
    /// </summary>
    private async Task ProjectAsync(OrderId id, IReadOnlyList<IOrderEvent> events, CancellationToken ct)
    {
        OrderView? view = null;
        if (events[0] is not OrderCreated)
            view = await context.Set<OrderView>().FirstOrDefaultAsync(v => v.Id == id, ct);

        foreach (var @event in events)
        {
            switch (@event)
            {
                case OrderCreated e:
                    var items = e.Items.Select(i => new OrderItem(
                        ProductId.From(i.ProductId), i.ProductName, Quantity.Of(i.Quantity), Money.Of(i.Price)));
                    view = OrderView.Create(
                        id,
                        CustomerId.From(e.CustomerId),
                        new Address(e.ShippingAddress.FirstName, e.ShippingAddress.LastName, e.ShippingAddress.Street,
                            e.ShippingAddress.City, e.ShippingAddress.PostalCode, e.ShippingAddress.Country),
                        (PaymentMethod)e.PaymentMethod,
                        e.CorrelationId,
                        items,
                        e.OccurredAt);
                    await context.Set<OrderView>().AddAsync(view, ct);
                    break;
                case CouponApplied e:
                    Require(view, id).ApplyDiscount(e.CouponCode, e.DiscountAmount);
                    break;
                case ShippingCostSet e:
                    Require(view, id).SetShippingCost(e.Amount);
                    break;
                case OrderConfirmed e:
                    Require(view, id).Confirm(e.TrackingNumber, e.OccurredAt);
                    break;
                case OrderShipped e:
                    Require(view, id).MarkShipped(e.OccurredAt);
                    break;
                case OrderDelivered e:
                    Require(view, id).MarkDelivered(e.OccurredAt);
                    break;
                case OrderCancelled e:
                    Require(view, id).Cancel(e.OccurredAt);
                    break;
            }
        }
    }

    private static OrderView Require(OrderView? view, OrderId id)
        => view ?? throw new InvalidOperationException(
            $"No read projection found for order {id.Value}; the stream is missing its OrderCreated event.");
}
