
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderSphere.Application.Abstraction;

namespace OrderSphere.Application.Behavior
{
    public sealed class TransactionBehavior<TRequest, TResponse>(IDbContext Context, ILogger<TransactionBehavior<TRequest, TResponse>> Logger)
        : IPipelineBehavior<TRequest, TResponse>
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            try
            {
                if (Context.IsTransactionRunning())
                    return await next(cancellationToken);

                await using var transaction = await Context.BeginTransactionAsync(cancellationToken);

                var response = await next(cancellationToken);

                await transaction.CommitAsync(cancellationToken);

                return response;
            }
            catch (Exception ex)
            {
                await Context.RollbackAsync(cancellationToken);
                Logger.LogError(ex, "An error occurred while processing transaction for request {RequestType}", typeof(TRequest).Name);
            }
        }
    }
}
