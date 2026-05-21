using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Domain.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        logger.LogDebug("Handling {RequestName}", requestName);

        var sw = Stopwatch.StartNew();
        try
        {
            var response = await next();
            sw.Stop();

            if (response is Result { IsFailure: true } failure)
                logger.LogWarning(
                    "{RequestName} failed in {ElapsedMs}ms — [{ErrorCode}] {ErrorDescription}",
                    requestName, sw.ElapsedMilliseconds, failure.Error.Code, failure.Error.Description);
            else
                logger.LogInformation("{RequestName} completed in {ElapsedMs}ms", requestName, sw.ElapsedMilliseconds);

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "{RequestName} threw after {ElapsedMs}ms", requestName, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
