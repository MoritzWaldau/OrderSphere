using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OrderSphere.Ordering.Domain.Entities;
using OrderSphere.Ordering.Infrastructure.Persistence;
using Xunit;

namespace OrderSphere.IntegrationTests.Api;

/// <summary>
/// Drives the Ordering API order and coupon endpoints end-to-end: the customer auth gate, the
/// role-based admin/staff policies, and the <c>Result&lt;T&gt;</c>→HTTP mapping. Checkout (which
/// orchestrates the Catalog and Basket clients) is covered separately by the flow tests.
/// </summary>
public sealed class OrderingApiTests : IClassFixture<OrderingApiFactory>
{
    private readonly OrderingApiFactory _factory;

    public OrderingApiTests(OrderingApiFactory factory) => _factory = factory;

    private HttpClient Client(string sub = "auth0|customer", string? roles = null)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.SubHeader, sub);
        if (roles is not null)
            client.DefaultRequestHeaders.Add(TestAuthHandler.RolesHeader, roles);
        return client;
    }


    [Fact]
    public async Task Orders_list_challenges_anonymous_with_401()
    {
        var response = await _factory.CreateClient().GetAsync("api/v1/orders");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Orders_list_is_empty_for_a_new_customer()
    {
        var orders = await Client(sub: "auth0|empty-orders").GetFromJsonAsync<object[]>("api/v1/orders");

        orders.Should().NotBeNull();
        orders!.Should().BeEmpty();
    }

    [Fact]
    public async Task Get_unknown_order_returns_404()
    {
        var response = await Client().GetAsync($"api/v1/orders/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Pending_correlation_returns_204()
    {
        var response = await Client().GetAsync($"api/v1/orders/correlation/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }


    [Fact]
    public async Task Admin_orders_forbids_a_plain_customer()
    {
        var response = await Client(sub: "auth0|plain").GetAsync("api/v1/admin/orders");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Admin_orders_allows_staff()
    {
        var response = await Client(sub: "auth0|csr", roles: "csr").GetAsync("api/v1/admin/orders");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Order_stats_are_available_to_staff()
    {
        var response = await Client(sub: "auth0|csr", roles: "csr").GetAsync("api/v1/admin/orders/stats");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Status_update_requires_order_manager_not_just_csr()
    {
        var body = new { newStatus = "Shipped" };

        var response = await Client(sub: "auth0|csr", roles: "csr")
            .PutAsJsonAsync($"api/v1/admin/orders/{Guid.NewGuid()}/status", body);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }


    [Fact]
    public async Task Saga_endpoint_forbids_a_plain_customer()
    {
        var response = await Client(sub: "auth0|plain").GetAsync($"api/v1/admin/sagas/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Saga_endpoint_returns_404_for_unknown_correlation()
    {
        var response = await Client(sub: "auth0|csr", roles: "csr")
            .GetAsync($"api/v1/admin/sagas/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Saga_endpoint_returns_projection_for_staff()
    {
        var correlationId = Guid.NewGuid();
        await SeedSagaAsync(correlationId);

        var response = await Client(sub: "auth0|csr", roles: "csr")
            .GetAsync($"api/v1/admin/sagas/{correlationId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("correlationId").GetGuid().Should().Be(correlationId);
        doc.RootElement.GetProperty("state").GetString().Should().Be("PaymentRequested");
    }

    private async Task SeedSagaAsync(Guid correlationId)
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<OrderingDbContext>();
        var saga = OrderSaga.Start(correlationId, Guid.NewGuid());
        saga.MarkPaymentRequested();
        context.OrderSagas.Add(saga);
        await context.SaveChangesAsync();
    }


    [Fact]
    public async Task Coupon_admin_create_then_list_roundtrips()
    {
        var admin = Client(sub: "auth0|admin", roles: "admin");
        var create = await admin.PostAsJsonAsync("api/v1/admin/coupons", new
        {
            code = "SAVE10",
            discountType = 0, // Flat
            value = 10m,
            minSubtotal = (decimal?)null,
            validFrom = (DateTime?)null,
            validUntil = (DateTime?)null,
            maxRedemptions = (int?)null,
            isActive = true,
        });

        create.StatusCode.Should().Be(HttpStatusCode.Created);

        var coupons = await admin.GetFromJsonAsync<object[]>("api/v1/admin/coupons");
        coupons!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Coupon_admin_endpoints_forbid_non_admin()
    {
        var response = await Client(sub: "auth0|csr", roles: "csr").GetAsync("api/v1/admin/coupons");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Validate_unknown_coupon_returns_a_failure_status()
    {
        var response = await Client(sub: "auth0|shopper")
            .GetAsync("api/v1/coupons/validate?code=NOPE&subtotal=50");

        // Unknown code maps to a NotFound/BadRequest failure, never a success.
        response.IsSuccessStatusCode.Should().BeFalse();
    }


    [Fact]
    public async Task Checkout_challenges_anonymous_with_401()
    {
        var response = await _factory.CreateClient().PostAsJsonAsync("api/v1/checkout", CheckoutBody());

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Checkout_accepts_a_cart_and_returns_a_correlation_id()
    {
        var response = await Client(sub: "auth0|checkout").PostAsJsonAsync("api/v1/checkout", CheckoutBody());

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        doc.RootElement.GetProperty("correlationId").GetGuid().Should().NotBeEmpty();
    }

    private static object CheckoutBody() => new
    {
        shippingAddress = new
        {
            firstName = "Erika",
            lastName = "Mustermann",
            street = "Hauptstraße 1",
            city = "Berlin",
            postalCode = "10115",
            country = "Deutschland",
        },
        paymentMethod = "CreditCard",
    };


    [Fact]
    public async Task Internal_purchase_check_accepts_an_authenticated_service_caller_and_reports_false()
    {
        // D4 — the endpoint now requires a valid client-credentials token; simulates the M2M
        // identity Catalog's HttpOrderingClient authenticates as. No role required.
        var response = await Client("service|catalog", roles: null).GetAsync(
            $"internal/customers/{Guid.NewGuid()}/purchased/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Be("false");
    }

    [Fact]
    public async Task Internal_purchase_check_challenges_anonymous_with_401()
    {
        var response = await _factory.CreateClient().GetAsync(
            $"internal/customers/{Guid.NewGuid()}/purchased/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
