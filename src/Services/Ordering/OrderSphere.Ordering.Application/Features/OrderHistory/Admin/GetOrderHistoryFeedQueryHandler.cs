using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Contracts;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.Ordering.Application.Abstractions;
using OrderSphere.Ordering.Application.Models;
using OrderSphere.Ordering.Domain.Errors;

namespace OrderSphere.Ordering.Application.Features.OrderHistory.Admin;

/// <summary>
/// Paged, cross-order activity feed over the <c>order_history</c> read-model, newest first.
/// This is the query the order write aggregate cannot serve efficiently — it would require
/// loading every order with its owned status-history collection. The denormalised view makes
/// it a single indexed scan. Optional <paramref name="CustomerEmail"/> filter narrows the feed
/// to one customer without joining the order tables.
/// </summary>
public sealed record GetOrderHistoryFeedQuery(int Page, int PageSize, string? CustomerEmail = null)
    : IQuery<Result<PagedResult<OrderHistoryEntryDto>>>;

public sealed class GetOrderHistoryFeedQueryHandler(
    IOrderingDbContext context,
    ILogger<GetOrderHistoryFeedQueryHandler> logger
) : IQueryHandler<GetOrderHistoryFeedQuery, Result<PagedResult<OrderHistoryEntryDto>>>
{
    private const int MaxPageSize = 100;
    private const int DefaultPageSize = 20;

    public async Task<Result<PagedResult<OrderHistoryEntryDto>>> Handle(
        GetOrderHistoryFeedQuery request, CancellationToken cancellationToken)
    {
        try
        {
            // Clamp paging defensively: this is a public read endpoint with no validator.
            var page = request.Page < 1 ? 1 : request.Page;
            var pageSize = request.PageSize is < 1 or > MaxPageSize ? DefaultPageSize : request.PageSize;

            var query = context.OrderHistory.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(request.CustomerEmail))
            {
                var email = request.CustomerEmail.Trim();
                query = query.Where(e => e.CustomerEmail == email);
            }

            var total = await query.CountAsync(cancellationToken);

            var items = await query
                .OrderByDescending(e => e.OccurredAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new OrderHistoryEntryDto(
                    e.Id, e.OrderId, e.CorrelationId, e.CustomerEmail,
                    e.PreviousStatus, e.NewStatus, e.OccurredAt))
                .ToListAsync(cancellationToken);

            return Result<PagedResult<OrderHistoryEntryDto>>.Success(
                new PagedResult<OrderHistoryEntryDto>(items, total, page, pageSize));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving order history feed (page {Page}, size {PageSize})",
                request.Page, request.PageSize);
            return Result<PagedResult<OrderHistoryEntryDto>>.Failure(OrderErrors.UnknownError);
        }
    }
}
