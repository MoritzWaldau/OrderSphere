using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Payment.Application.Abstractions;
using OrderSphere.Payment.Application.Models;
using OrderSphere.Payment.Domain.Entities;
using Xunit;

namespace OrderSphere.IntegrationTests.Api;

/// <summary>
/// Exercises the Payment Minimal-API surface: the authenticated read endpoints, the internal
/// lookup (D4 — now requires a client-credentials token), and the <c>Result&lt;T&gt;</c>→HTTP
/// mapping for found/not-found payments.
/// </summary>
public sealed class PaymentApiTests : IClassFixture<PaymentApiFactory>
{
    private readonly PaymentApiFactory _factory;

    public PaymentApiTests(PaymentApiFactory factory) => _factory = factory;

    private HttpClient AuthedClient()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.SubHeader, "auth0|payer");
        return client;
    }

    private async Task<(Guid paymentId, Guid orderId)> SeedPaymentAsync()
    {
        var orderId = Guid.NewGuid();
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<IPaymentDbContext>();
        var record = new PaymentRecord(OrderId.From(orderId), 49.99m, "EUR", "CreditCard", "payer@example.com", Guid.NewGuid());
        record.MarkCaptured("CC-12345");
        context.Payments.Add(record);
        await context.SaveChangesAsync(CancellationToken.None);
        return (record.Id.Value, orderId);
    }

    [Fact]
    public async Task Public_endpoint_challenges_anonymous_with_401()
    {
        var response = await _factory.CreateClient().GetAsync($"api/v1/payments/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Get_unknown_payment_returns_404()
    {
        var response = await AuthedClient().GetAsync($"api/v1/payments/{Guid.NewGuid()}");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Get_payment_by_id_returns_the_seeded_record()
    {
        var (paymentId, _) = await SeedPaymentAsync();

        var payment = await AuthedClient().GetFromJsonAsync<PaymentDto>($"api/v1/payments/{paymentId}");

        payment.Should().NotBeNull();
        payment!.Id.Should().Be(paymentId);
        payment.Status.Should().Be("Captured");
        payment.TransactionId.Should().Be("CC-12345");
    }

    [Fact]
    public async Task Get_payment_by_order_returns_the_seeded_record()
    {
        var (paymentId, orderId) = await SeedPaymentAsync();

        var payment = await AuthedClient().GetFromJsonAsync<PaymentDto>($"api/v1/payments/by-order/{orderId}");

        payment!.Id.Should().Be(paymentId);
        payment.OrderId.Should().Be(orderId);
    }

    [Fact]
    public async Task Internal_endpoint_accepts_an_authenticated_service_caller()
    {
        var (_, orderId) = await SeedPaymentAsync();

        // D4 — any authenticated caller is accepted (no role required); simulates the M2M
        // identity a future internal caller would authenticate as.
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add(TestAuthHandler.SubHeader, "service|internal-caller");
        var response = await client.GetAsync($"internal/payments/by-order/{orderId}");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Internal_endpoint_challenges_anonymous_with_401()
    {
        var (_, orderId) = await SeedPaymentAsync();

        var response = await _factory.CreateClient().GetAsync($"internal/payments/by-order/{orderId}");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
