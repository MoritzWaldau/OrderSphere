using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.Ordering.Application.Abstractions;
using OrderSphere.Ordering.Application.Models;
using OrderSphere.Ordering.Domain.Errors;

namespace OrderSphere.Ordering.Application.Features.Saga;

/// <summary>
/// Loads the saga projection for a correlation id. Returns <c>null</c> in the success value
/// when no saga exists yet (the order has not been processed by the worker).
/// </summary>
public sealed record GetSagaByCorrelationIdQuery(Guid CorrelationId)
    : IQuery<Result<SagaDto?>>;

public sealed class GetSagaByCorrelationIdQueryHandler(
    IOrderingDbContext context,
    ILogger<GetSagaByCorrelationIdQueryHandler> logger
) : IQueryHandler<GetSagaByCorrelationIdQuery, Result<SagaDto?>>
{
    public async Task<Result<SagaDto?>> Handle(GetSagaByCorrelationIdQuery request, CancellationToken cancellationToken)
    {
        try
        {
            var saga = await context.OrderSagas
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.CorrelationId == request.CorrelationId, cancellationToken);

            if (saga is null)
                return Result<SagaDto?>.Success(null);

            return Result<SagaDto?>.Success(new SagaDto(
                saga.CorrelationId,
                saga.OrderId,
                saga.State.ToString(),
                saga.CreatedAt,
                saga.UpdatedAt,
                saga.PaymentRequestedAt,
                saga.CompletedAt,
                saga.LastError));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving saga by correlationId {CorrelationId}", request.CorrelationId);
            return Result<SagaDto?>.Failure(OrderErrors.UnknownError);
        }
    }
}
