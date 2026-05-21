using Microsoft.Extensions.Logging;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Order.ProcessOrder;

/// <summary>
/// This handler is no longer used by OrderSphere.Worker (Phase 3+).
/// Order processing now lives in OrderSphere.Ordering.Worker.
/// Retained in the monolith to avoid breaking any remaining references.
/// </summary>
public sealed class ProcessOrderCommandHandler(
    ILogger<ProcessOrderCommandHandler> logger
) : ICommandHandler<ProcessOrderCommand, Result<Guid>>
{
    public Task<Result<Guid>> Handle(ProcessOrderCommand request, CancellationToken cancellationToken)
    {
        logger.LogWarning(
            "ProcessOrderCommandHandler is deprecated. Order processing has moved to OrderSphere.Ordering.Worker.");
        return Task.FromResult(Result<Guid>.Failure(
            new Error("Order.Deprecated", "Use OrderSphere.Ordering.Worker for order processing.")));
    }
}
