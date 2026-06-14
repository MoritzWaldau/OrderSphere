using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;
using OrderSphere.BuildingBlocks.Diagnostics;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.BuildingBlocks.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        logger.LogDebug("Handling {RequestName}", requestName);

        using var activity = ApplicationDiagnostics.ActivitySource.StartActivity(requestName);
        var sw = Stopwatch.StartNew();
        var outcome = "success";
        try
        {
            var response = await next();
            sw.Stop();

            if (response is Result { IsFailure: true } failure)
            {
                outcome = "failure";
                activity?.SetTag("request.outcome", outcome);
                activity?.SetTag("error.code", failure.Error.Code);
                logger.LogWarning(
                    "{RequestName} failed in {ElapsedMs}ms — [{ErrorCode}] {ErrorDescription}",
                    requestName, sw.ElapsedMilliseconds, failure.Error.Code, failure.Error.Description);
            }
            else
            {
                logger.LogInformation("{RequestName} completed in {ElapsedMs}ms", requestName, sw.ElapsedMilliseconds);
            }

            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            outcome = "exception";
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            logger.LogError(ex, "{RequestName} threw after {ElapsedMs}ms", requestName, sw.ElapsedMilliseconds);
            throw;
        }
        finally
        {
            ApplicationDiagnostics.RequestDuration.Record(
                sw.Elapsed.TotalMilliseconds,
                new KeyValuePair<string, object?>("request", requestName),
                new KeyValuePair<string, object?>("outcome", outcome));
        }
    }
}
