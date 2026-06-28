using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Ordering.Application.Abstractions;
using OrderSphere.Ordering.Application.Models;

namespace OrderSphere.Ordering.Application.Features.Returns.GetReturnsByCustomer;

public sealed record GetReturnsByCustomerQuery(Guid CustomerId)
    : IQuery<Result<IReadOnlyList<ReturnRequestDto>>>;

public sealed class GetReturnsByCustomerQueryHandler(IOrderingDbContext context)
    : IQueryHandler<GetReturnsByCustomerQuery, Result<IReadOnlyList<ReturnRequestDto>>>
{
    public async Task<Result<IReadOnlyList<ReturnRequestDto>>> Handle(
        GetReturnsByCustomerQuery request, CancellationToken ct)
    {
        var returns = await context.ReturnRequests
            .AsNoTracking()
            .Include(r => r.Items)
            .Where(r => r.CustomerId == CustomerId.From(request.CustomerId))
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync(ct);

        var dtos = returns.Select(r => r.ToDto()).ToList();
        return Result<IReadOnlyList<ReturnRequestDto>>.Success(dtos);
    }
}
