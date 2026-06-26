using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace OrderSphere.Bff.Tests;

/// <summary>
/// Integration tests for GET /bff/exchange-rates — the anonymous endpoint the WASM client uses to
/// fetch the base currency and the static rate table for presentation-only currency conversion.
/// </summary>
public sealed class ExchangeRatesEndpointTests(BffWebApplicationFactory factory)
    : IClassFixture<BffWebApplicationFactory>
{
    [Fact]
    public async Task Get_ReturnsOk()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/bff/exchange-rates");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Get_ReturnsEurBaseCurrency()
    {
        var body = await GetBodyAsync();

        body.GetProperty("baseCurrency").GetString().Should().Be("EUR");
    }

    [Fact]
    public async Task Get_RatesIncludeBaseCurrencyAtRateOne()
    {
        var body = await GetBodyAsync();

        var rates = body.GetProperty("rates");
        rates.GetProperty("EUR").GetDecimal().Should().Be(1m);
    }

    [Fact]
    public async Task Get_RatesIncludeConfiguredForeignCurrency()
    {
        var body = await GetBodyAsync();

        var rates = body.GetProperty("rates");
        // USD is configured in appsettings.json; its rate must be present and positive.
        rates.GetProperty("USD").GetDecimal().Should().BeGreaterThan(0m);
    }

    private async Task<JsonElement> GetBodyAsync()
    {
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.GetAsync("/bff/exchange-rates");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }
}
