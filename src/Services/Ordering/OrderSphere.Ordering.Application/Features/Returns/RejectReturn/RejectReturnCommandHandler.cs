using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Ordering.Application.Abstractions;
using OrderSphere.Ordering.Domain.Errors;

namespace OrderSphere.Ordering.Application.Features.Returns.RejectReturn;

/// <summary>
/// Rejects a pending return request. Terminal: no refund is triggered.
/// </summary>
public sealed class RejectReturnCommandHandler(IOrderingDbContext context)
    : ICommandHandler<RejectReturnCommand, Result>
{
    public async Task<Result> Handle(RejectReturnCommand request, CancellationToken ct)
    {
        var returnRequest = await context.ReturnRequests
            .AsTracking()
            .FirstOrDefaultAsync(r => r.Id == ReturnRequestId.From(request.ReturnRequestId), ct);

        if (returnRequest is null)
            return Result.Failure(ReturnErrors.NotFound);

        var transition = returnRequest.Reject(request.Note, DateTime.UtcNow);
        if (transition.IsFailure)
            return transition;

        await context.SaveChangesAsync(ct);
        return Result.Success();
    }
}
