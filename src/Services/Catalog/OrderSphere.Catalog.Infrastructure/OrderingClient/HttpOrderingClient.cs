using System.Net.Http.Json;
using Microsoft.Extensions.Logging;

namespace OrderSphere.Catalog.Infrastructure.OrderingClient;

/// <summary>
/// Calls the Ordering service's internal purchase-verification endpoint.
/// Fails closed: any transport error is treated as "not purchased" so a degraded
/// Ordering service cannot let unverified customers post reviews.
/// </summary>
public sealed class HttpOrderingClient(HttpClient httpClient, ILogger<HttpOrderingClient> logger) : IOrderingClient
{
    public async Task<bool> HasPurchasedAsync(Guid customerId, Guid productId, CancellationToken ct = default)
    {
        try
        {
            var response = await httpClient.GetAsync(
                $"/internal/customers/{customerId}/purchased/{productId}", ct);

            if (!response.IsSuccessStatusCode)
                return false;

            return await response.Content.ReadFromJsonAsync<bool>(ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error verifying purchase of product {ProductId} by customer {CustomerId}",
                productId, customerId);
            return false;
        }
    }
}
