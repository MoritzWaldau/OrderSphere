using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using OrderSphere.Mcp.Server.Gateway;

namespace OrderSphere.Mcp.Server.Tools;

// User-scoped payment tools. Resolves the payment of an order belonging to the
// authenticated caller via the forwarded bearer token. Read-only.
[McpServerToolType]
public sealed class PaymentTools
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [McpServerTool(Name = "get_payment_status")]
    [Description("Get the payment status for one of the current customer's orders: amount, currency, method, status, and failure reason if any.")]
    public static async Task<string> GetPaymentStatusAsync(
        IOrderSphereGateway gateway,
        [Description("The order id (GUID) whose payment should be looked up.")] Guid orderId,
        CancellationToken ct = default)
    {
        var payment = await gateway.GetPaymentByOrderAsync(orderId, ct);
        if (payment is null)
        {
            return $"No payment found for order '{orderId}' (or the caller is not authorized).";
        }

        var result = new
        {
            payment.OrderId,
            payment.Amount,
            payment.Currency,
            payment.PaymentMethod,
            payment.Status,
            payment.FailureReason,
            payment.CreatedAt
        };
        return JsonSerializer.Serialize(result, Json);
    }
}
