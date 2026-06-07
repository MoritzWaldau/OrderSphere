using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using OrderSphere.Mcp.Server.Gateway;

namespace OrderSphere.Mcp.Server.Tools;

// User-scoped ordering tools. Data is resolved for the authenticated caller via
// the forwarded bearer token; the downstream Ordering service derives the customer
// id from the JWT. Without a valid user token the gateway returns no data.
[McpServerToolType]
public sealed class OrderingTools
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [McpServerTool(Name = "get_my_orders")]
    [Description("List the current customer's orders (most recent first) with status, total, and item count.")]
    public static async Task<string> GetMyOrdersAsync(
        IOrderSphereGateway gateway,
        CancellationToken ct = default)
    {
        var orders = await gateway.GetMyOrdersAsync(ct);
        var summary = orders
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new
            {
                o.Id,
                o.Status,
                o.Total,
                o.PaymentMethod,
                o.TrackingNumber,
                ItemCount = o.Items.Count,
                o.CreatedAt
            });
        return JsonSerializer.Serialize(new { count = orders.Count, orders = summary }, Json);
    }

    [McpServerTool(Name = "get_order_status")]
    [Description("Get the detailed status of a single order belonging to the current customer, including line items and shipping address.")]
    public static async Task<string> GetOrderStatusAsync(
        IOrderSphereGateway gateway,
        [Description("The order id (GUID).")] Guid orderId,
        CancellationToken ct = default)
    {
        var order = await gateway.GetOrderAsync(orderId, ct);
        return order is null
            ? $"No order '{orderId}' found for the current customer."
            : JsonSerializer.Serialize(order, Json);
    }

    [McpServerTool(Name = "validate_coupon")]
    [Description("Check whether a coupon code is valid for a given order subtotal and return the discount amount.")]
    public static async Task<string> ValidateCouponAsync(
        IOrderSphereGateway gateway,
        [Description("The coupon code to validate.")] string code,
        [Description("The order subtotal the coupon would apply to.")] decimal subtotal,
        CancellationToken ct = default)
    {
        var result = await gateway.ValidateCouponAsync(code, subtotal, ct);
        return result is null
            ? $"Could not validate coupon '{code}'."
            : JsonSerializer.Serialize(result, Json);
    }
}
