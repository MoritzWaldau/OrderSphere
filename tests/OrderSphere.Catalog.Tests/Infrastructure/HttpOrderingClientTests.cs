using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using OrderSphere.Catalog.Infrastructure.OrderingClient;

namespace OrderSphere.Catalog.Tests.Infrastructure;

public sealed class HttpOrderingClientTests
{
    private sealed class FakeHttpHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly object? _body;

        public HttpRequestMessage? LastRequest { get; private set; }

        public FakeHttpHandler(HttpStatusCode statusCode, object? body = null)
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

    private static (HttpOrderingClient Client, FakeHttpHandler Handler) Build(
        HttpStatusCode status, object? body = null)
    {
        var handler = new FakeHttpHandler(status, body);
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://ordering") };
        return (new HttpOrderingClient(http, Substitute.For<ILogger<HttpOrderingClient>>()), handler);
    }


    [Fact]
    public async Task HasPurchased_ReturnsTrue_WhenServerRespondsTrue()
    {
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var (client, handler) = Build(HttpStatusCode.OK, true);

        var result = await client.HasPurchasedAsync(customerId, productId);

        result.Should().BeTrue();
        handler.LastRequest!.Method.Should().Be(HttpMethod.Get);
        handler.LastRequest.RequestUri!.PathAndQuery
            .Should().Be($"/internal/customers/{customerId}/purchased/{productId}");
    }

    [Fact]
    public async Task HasPurchased_ReturnsFalse_WhenServerRespondsFalse()
    {
        var (client, _) = Build(HttpStatusCode.OK, false);

        var result = await client.HasPurchasedAsync(Guid.NewGuid(), Guid.NewGuid());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasPurchased_ReturnsFalse_OnNonSuccess()
    {
        var (client, _) = Build(HttpStatusCode.NotFound);

        var result = await client.HasPurchasedAsync(Guid.NewGuid(), Guid.NewGuid());

        result.Should().BeFalse();
    }

    [Fact]
    public async Task HasPurchased_ReturnsFalse_OnTransportFailure()
    {
        // Simulate network error by throwing from the handler
        var http = new HttpClient(new ThrowingHandler()) { BaseAddress = new Uri("http://ordering") };
        var client = new HttpOrderingClient(http, Substitute.For<ILogger<HttpOrderingClient>>());

        var result = await client.HasPurchasedAsync(Guid.NewGuid(), Guid.NewGuid());

        result.Should().BeFalse();
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
            => throw new HttpRequestException("Network error");
    }
}
