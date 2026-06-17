using System.Diagnostics;

namespace OrderSphere.Web.Services;

/// <summary>
/// Outermost HTTP pipeline handler. Logs failed and slow requests for client-side
/// observability. Streaming requests (advisor SSE) are unaffected: timing is measured
/// up to the response headers only, and caller cancellation is not treated as an error.
/// </summary>
public sealed class LoggingHandler(ILogger<LoggingHandler> logger) : DelegatingHandler
{
    private const long SlowThresholdMs = 2000;

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var path = request.RequestUri?.PathAndQuery;
        var sw = Stopwatch.StartNew();
        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            sw.Stop();

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("HTTP {Method} {Path} -> {Status} in {Elapsed}ms",
                    request.Method, path, (int)response.StatusCode, sw.ElapsedMilliseconds);
            }
            else if (sw.ElapsedMilliseconds > SlowThresholdMs)
            {
                logger.LogInformation("Slow HTTP {Method} {Path} -> {Status} in {Elapsed}ms",
                    request.Method, path, (int)response.StatusCode, sw.ElapsedMilliseconds);
            }

            return response;
        }
        catch (OperationCanceledException)
        {
            // Caller/navigation cancellation — not a fault.
            throw;
        }
        catch (Exception ex)
        {
            sw.Stop();
            logger.LogError(ex, "HTTP {Method} {Path} failed after {Elapsed}ms",
                request.Method, path, sw.ElapsedMilliseconds);
            throw;
        }
    }
}
