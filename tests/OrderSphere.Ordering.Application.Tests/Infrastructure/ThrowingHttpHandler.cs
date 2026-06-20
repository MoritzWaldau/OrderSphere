namespace OrderSphere.Ordering.Application.Tests.Infrastructure;

/// <summary>Throws on every send, exercising an HTTP client's transport-failure (catch) branch.</summary>
internal sealed class ThrowingHttpHandler : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
        => throw new HttpRequestException("Simulated transport failure.");
}
