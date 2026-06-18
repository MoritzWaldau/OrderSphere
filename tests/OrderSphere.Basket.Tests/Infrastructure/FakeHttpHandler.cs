using System.Net;
using System.Net.Http.Json;

namespace OrderSphere.Basket.Tests.Infrastructure;

internal sealed class FakeHttpHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly object? _body;

    public HttpRequestMessage? LastRequest { get; private set; }

    public FakeHttpHandler(HttpStatusCode statusCode = HttpStatusCode.OK, object? body = null)
    {
        _statusCode = statusCode;
        _body = body;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        LastRequest = request;
        var response = new HttpResponseMessage(_statusCode);
        if (_body is not null)
            response.Content = JsonContent.Create(_body);
        return Task.FromResult(response);
    }
}
