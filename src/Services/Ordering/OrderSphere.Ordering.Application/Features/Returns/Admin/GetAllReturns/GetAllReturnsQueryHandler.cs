using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.Ordering.Application.Abstractions;
using OrderSphere.Ordering.Application.Models;
using OrderSphere.Ordering.Domain.Enums;

namespace OrderSphere.Ordering.Application.Features.Returns.Admin.GetAllReturns;

public sealed record GetAllReturnsQuery(ReturnStatus? Status)
    : IQuery<Result<IReadOnlyList<ReturnRequestDto>>>;

public sealed class GetAllReturnsQueryHandler(IOrderingDbContext context)
    : IQueryHandler<GetAllReturnsQuery, Result<IReadOnlyList<ReturnRequestDto>>>
{
    public async Task<Result<IReadOnlyList<ReturnRequestDto>>> Handle(
        GetAllReturnsQuery request, CancellationToken ct)
    {
        var query = context.ReturnRequests
            .AsNoTracking()
            .Include(r => r.Items)
            .AsQueryable();

        if (request.Status is { } status)
            query = query.Where(r => r.Status == status);

        var returns = await query
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync(ct);

        var dtos = returns.Select(r => r.ToDto()).ToList();
        return Result<IReadOnlyList<ReturnRequestDto>>.Success(dtos);
    }
}
