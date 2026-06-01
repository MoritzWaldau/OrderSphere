using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Payment.Application.Abstractions;
using OrderSphere.Payment.Application.Models;
using OrderSphere.Payment.Domain.Errors;

namespace OrderSphere.Payment.Application.Features.Payments;

/// <summary>
/// Loads a single payment record by its identifier.
/// </summary>
public sealed record GetPaymentByIdQuery(Guid PaymentId) : IQuery<Result<PaymentDto>>;

public sealed class GetPaymentByIdQueryHandler(
    IPaymentDbContext context
) : IQueryHandler<GetPaymentByIdQuery, Result<PaymentDto>>
{
    public async Task<Result<PaymentDto>> Handle(GetPaymentByIdQuery request, CancellationToken cancellationToken)
    {
        var payment = await context.Payments
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == PaymentId.From(request.PaymentId), cancellationToken);

        return payment is null
            ? Result<PaymentDto>.Failure(PaymentErrors.PaymentNotFound)
            : Result<PaymentDto>.Success(payment.ToDto());
    }
}
