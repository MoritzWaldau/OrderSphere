using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderSphere.Application.Abstraction;
using OrderSphere.Application.Models;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Entities;
using OrderSphere.Domain.Errors;
using OrderSphere.Domain.Primitives;
using OrderSphere.Domain.Services;

namespace OrderSphere.Application.Features.Order.ProcessOrder;

public sealed class ProcessOrderCommandHandler(
    IDbContext context,
    IUserEmailLookup userEmailLookup,
    IEmailService emailService,
    ILogger<ProcessOrderCommandHandler> logger
) : ICommandHandler<ProcessOrderCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(ProcessOrderCommand request, CancellationToken cancellationToken)
    {
        var evt = request.Event;
        var checkout = evt.CheckoutCart;

        // Idempotency check: if the worker already processed this CorrelationId,
        // the order exists. Service Bus may redeliver messages (at-least-once),
        // so we must treat this case as success without creating a duplicate.
        var existingOrderId = await context.Orders
            .Where(o => o.CorrelationId == evt.CorrelationId)
            .Select(o => (Guid?)o.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (existingOrderId is not null)
        {
            logger.LogInformation(
                "Duplicate message for CorrelationId {CorrelationId} ignored. Existing OrderId: {OrderId}",
                evt.CorrelationId,
                existingOrderId);
            return Result<Guid>.Success(existingOrderId.Value);
        }

        try
        {
            await context.BeginTransactionAsync(cancellationToken);

            var orderItems = evt.Items
                .Select(i => new OrderItem(i.ProdicutId, i.Quantity, i.Price))
                .ToList();

            var order = new Domain.Entities.Order(
                checkout.CustomerId,
                checkout.ShippingAddress,
                checkout.PaymentMethod,
                orderItems,
                evt.CorrelationId);

            order.Confirm(TrackingNumberGenerator.Generate());

            await context.Orders.AddAsync(order, cancellationToken);
            await context.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Order {OrderId} created for customer {CustomerId} with TrackingNumber {TrackingNumber}. CorrelationId: {CorrelationId}",
                order.Id,
                order.CustomerId,
                order.TrackingNumber,
                evt.CorrelationId);

            await TrySendConfirmationMail(order, evt, cancellationToken);

            return Result<Guid>.Success(order.Id);
        }
        catch (DbUpdateException dbEx) when (IsUniqueConstraintViolation(dbEx))
        {
            // Race condition: another concurrent worker (or retry) inserted the order
            // between our check above and our insert. Treat as success.
            await context.RollbackAsync(cancellationToken);

            var raceWinnerId = await context.Orders
                .Where(o => o.CorrelationId == evt.CorrelationId)
                .Select(o => (Guid?)o.Id)
                .FirstOrDefaultAsync(cancellationToken);

            logger.LogWarning(dbEx,
                "Concurrent insert detected for CorrelationId {CorrelationId}. Existing OrderId: {OrderId}",
                evt.CorrelationId,
                raceWinnerId);

            return raceWinnerId is not null
                ? Result<Guid>.Success(raceWinnerId.Value)
                : Result<Guid>.Failure(OrderErrors.UnknownError);
        }
        catch (Exception ex)
        {
            await context.RollbackAsync(cancellationToken);
            logger.LogError(ex,
                "Failed to process order for customer {CustomerId}. CorrelationId: {CorrelationId}",
                checkout.CustomerId,
                evt.CorrelationId);

            return Result<Guid>.Failure(OrderErrors.UnknownError);
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException ex)
    {
        // Postgres unique violation SQLSTATE = 23505. Detected by message inspection
        // to avoid a hard reference to Npgsql in the Application layer.
        var inner = ex.InnerException;
        return inner is not null
            && (inner.Message.Contains("23505", StringComparison.Ordinal)
                || inner.GetType().Name.Contains("PostgresException", StringComparison.Ordinal)
                    && inner.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase));
    }

    private async Task TrySendConfirmationMail(
        Domain.Entities.Order order,
        Models.Events.CheckoutCartEvent evt,
        CancellationToken cancellationToken)
    {
        try
        {
            var email = await userEmailLookup.GetEmailAsync(order.CustomerId, cancellationToken);
            if (string.IsNullOrWhiteSpace(email))
            {
                logger.LogWarning(
                    "No email address found for customer {CustomerId}. Skipping confirmation mail.",
                    order.CustomerId);
                return;
            }

            var productIds = evt.Items.Select(i => i.ProdicutId).Distinct().ToList();
            var productNames = await context.Products
                .Where(p => productIds.Contains(p.Id))
                .Select(p => new { p.Id, p.Name })
                .ToDictionaryAsync(p => p.Id, p => p.Name, cancellationToken);

            var lines = evt.Items
                .Select(i => new OrderConfirmationLine(
                    productNames.TryGetValue(i.ProdicutId, out var name) ? name : "Unbekanntes Produkt",
                    i.Quantity,
                    i.Price))
                .ToList();

            var total = evt.Items.Sum(i => i.Price * i.Quantity);

            var data = new OrderConfirmationData(
                order.Id,
                order.TrackingNumber!,
                order.ShippingAddress,
                lines,
                total);

            await emailService.SendOrderConfirmationAsync(email, data);

            logger.LogInformation("Order confirmation email sent for order {OrderId} to {Email}",
                order.Id, email);
        }
        catch (Exception mailEx)
        {
            logger.LogWarning(mailEx,
                "Order {OrderId} was created but the confirmation mail failed.",
                order.Id);
        }
    }
}
