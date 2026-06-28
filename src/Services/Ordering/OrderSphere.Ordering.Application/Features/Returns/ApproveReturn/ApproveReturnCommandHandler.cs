using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Contracts.Events;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Ordering.Application.Abstractions;
using OrderSphere.Ordering.Domain.Errors;

namespace OrderSphere.Ordering.Application.Features.Returns.ApproveReturn;

/// <summary>
/// Approves a pending return request and stages a <see cref="RefundRequestedIntegrationEvent"/> in
/// the outbox so Payment refunds the captured payment. The state transition and the outbox row
/// commit in one SaveChanges, so an approved request always has its refund signal queued.
/// </summary>
public sealed class ApproveReturnCommandHandler(IOrderingDbContext context)
    : ICommandHandler<ApproveReturnCommand, Result>
{
    public async Task<Result> Handle(ApproveReturnCommand request, CancellationToken ct)
    {
        var returnRequest = await context.ReturnRequests
            .AsTracking()
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == ReturnRequestId.From(request.ReturnRequestId), ct);

        if (returnRequest is null)
            return Result.Failure(ReturnErrors.NotFound);

        var transition = returnRequest.Approve(request.Note, DateTime.UtcNow);
        if (transition.IsFailure)
            return transition;

        // Correlate the refund with the originating order's saga correlation id for trace continuity.
        var correlationId = await context.Orders
            .AsNoTracking()
            .Where(o => o.Id == returnRequest.OrderId)
            .Select(o => o.CorrelationId)
            .FirstOrDefaultAsync(ct);

        context.AddOutboxMessage(
            nameof(RefundRequestedIntegrationEvent),
            JsonSerializer.Serialize(new RefundRequestedIntegrationEvent
            {
                CorrelationId = correlationId,
                ReturnRequestId = returnRequest.Id.Value,
                OrderId = returnRequest.OrderId.Value,
                Amount = returnRequest.RefundAmount,
                Currency = returnRequest.Currency,
                Reason = returnRequest.Reason
            }));

        await context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
