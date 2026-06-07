using System.Net;
using System.Text;
using FluentAssertions;
using OrderSphere.Mcp.Server.Gateway;
using Xunit;

namespace OrderSphere.Mcp.Tests;

// Locks the exact /api/v1 request paths the gateway issues against the API Gateway
// contract. These assertions are what catch path drift (e.g. a singular/plural
// mismatch) that the tool-level tests — which mock IOrderSphereGateway — cannot see.
public sealed class OrderSphereGatewayTests
{
    private sealed class RecordingHandler(HttpStatusCode status, string json) : HttpMessageHandler
    {
        public string? LastPathAndQuery { get; private set; }
        public HttpMethod? LastMethod { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastPathAndQuery = request.RequestUri!.PathAndQuery;
            LastMethod = request.Method;
            return Task.FromResult(new HttpResponseMessage(status)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
    }

    private static (OrderSphereGateway Gateway, RecordingHandler Handler) Build(
        string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        var handler = new RecordingHandler(status, json);
        var http = new HttpClient(handler) { BaseAddress = new Uri("http://gateway") };
        return (new OrderSphereGateway(http), handler);
    }

    [Fact]
    public async Task GetProducts_RequestsPagedProductsPath()
    {
        var (gateway, handler) = Build("""{"items":[],"totalCount":0,"page":1,"pageSize":50}""");

        var result = await gateway.GetProductsAsync(1, 50);

        handler.LastPathAndQuery.Should().Be("/api/v1/products?page=1&pageSize=50");
        result.PageSize.Should().Be(50);
    }

    [Fact]
    public async Task GetProductBySlug_RequestsSlugPath()
    {
        var (gateway, handler) = Build(
            """{"id":"11111111-1111-1111-1111-111111111111","name":"X","slug":"x","description":"d","price":1,"stock":1,"categoryId":"22222222-2222-2222-2222-222222222222","categoryName":"C","sku":"S","imageUrl":null,"isActive":true}""");

        var product = await gateway.GetProductBySlugAsync("mens-trail-runner");

        handler.LastPathAndQuery.Should().Be("/api/v1/products/mens-trail-runner");
        product!.Slug.Should().Be("x");
    }

    [Fact]
    public async Task GetCategories_RequestsPagedCategoriesPath()
    {
        var (gateway, handler) = Build("""{"items":[],"totalCount":0,"page":1,"pageSize":50}""");

        await gateway.GetCategoriesAsync(1, 50);

        handler.LastPathAndQuery.Should().Be("/api/v1/categories?page=1&pageSize=50");
    }

    [Fact]
    public async Task GetMyOrders_RequestsOrdersPath()
    {
        var (gateway, handler) = Build("[]");

        await gateway.GetMyOrdersAsync();

        handler.LastPathAndQuery.Should().Be("/api/v1/orders");
    }

    [Fact]
    public async Task GetOrder_RequestsOrderByIdPath()
    {
        var id = Guid.NewGuid();
        var (gateway, handler) = Build("", HttpStatusCode.NotFound);

        var order = await gateway.GetOrderAsync(id);

        handler.LastPathAndQuery.Should().Be($"/api/v1/orders/{id}");
        order.Should().BeNull();
    }

    [Fact]
    public async Task ValidateCoupon_RequestsPluralCouponsPath()
    {
        var (gateway, handler) = Build("""{"isValid":true,"discountAmount":15,"message":null}""");

        var result = await gateway.ValidateCouponAsync("SUMMER15", 100m);

        // Must be plural — matches the Gateway route and the Ordering endpoint.
        handler.LastPathAndQuery.Should().Be("/api/v1/coupons/validate?code=SUMMER15&subtotal=100");
        result!.IsValid.Should().BeTrue();
    }

    [Fact]
    public async Task GetMyProfile_RequestsProfilePath_AndReturnsNullOn404()
    {
        var (gateway, handler) = Build("", HttpStatusCode.Unauthorized);

        var profile = await gateway.GetMyProfileAsync();

        handler.LastPathAndQuery.Should().Be("/api/v1/profile");
        profile.Should().BeNull();
    }

    [Fact]
    public async Task GetMyAddresses_RequestsAddressesPath()
    {
        var (gateway, handler) = Build("[]");

        var addresses = await gateway.GetMyAddressesAsync();

        handler.LastPathAndQuery.Should().Be("/api/v1/profile/addresses");
        addresses.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMyAddresses_ReturnsEmpty_OnUnauthorized()
    {
        var (gateway, _) = Build("", HttpStatusCode.Unauthorized);

        var addresses = await gateway.GetMyAddressesAsync();

        addresses.Should().BeEmpty();
    }

    [Fact]
    public async Task GetMyCart_RequestsCartPath()
    {
        var (gateway, handler) = Build(
            """{"customerId":"33333333-3333-3333-3333-333333333333","items":[]}""");

        var cart = await gateway.GetMyCartAsync();

        handler.LastPathAndQuery.Should().Be("/api/v1/cart");
        cart!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPaymentByOrder_RequestsByOrderPath()
    {
        var orderId = Guid.NewGuid();
        var (gateway, handler) = Build("", HttpStatusCode.NotFound);

        var payment = await gateway.GetPaymentByOrderAsync(orderId);

        handler.LastPathAndQuery.Should().Be($"/api/v1/payments/by-order/{orderId}");
        payment.Should().BeNull();
    }
}
