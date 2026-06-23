using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.Ordering.Application.Abstractions;
using OrderSphere.Ordering.Application.Models;
using OrderSphere.Ordering.Domain.Errors;

namespace OrderSphere.Ordering.Application.Features.OrderHistory;

/// <summary>
/// Returns the status-transition timeline for a single order from the <c>order_history</c>
/// read-model, oldest entry first. Reads the materialised view directly — no join to the
/// order aggregate.
/// </summary>
public sealed record GetOrderHistoryForOrderQuery(Guid OrderId)
    : IQuery<Result<IReadOnlyList<OrderHistoryEntryDto>>>;

public sealed class GetOrderHistoryForOrderQueryHandler(
    IOrderingDbContext context,
    ILogger<GetOrderHistoryForOrderQueryHandler> logger
) : IQueryHandler<GetOrderHistoryForOrderQuery, Result<IReadOnlyList<OrderHistoryEntryDto>>>
{
    public async Task<Result<IReadOnlyList<OrderHistoryEntryDto>>> Handle(
        GetOrderHistoryForOrderQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var entries = await context.OrderHistory
                .AsNoTracking()
                .Where(e => e.OrderId == request.OrderId)
                .OrderBy(e => e.OccurredAt)
                .Select(e => new OrderHistoryEntryDto(
                    e.Id, e.OrderId, e.CorrelationId, e.CustomerEmail,
                    e.PreviousStatus, e.NewStatus, e.OccurredAt))
                .ToListAsync(cancellationToken);

            return Result<IReadOnlyList<OrderHistoryEntryDto>>.Success(entries);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving order history for order {OrderId}", request.OrderId);
            return Result<IReadOnlyList<OrderHistoryEntryDto>>.Failure(OrderErrors.UnknownError);
        }
    }
}
