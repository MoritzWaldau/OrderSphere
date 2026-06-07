using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using OrderSphere.Mcp.Server.Gateway;

namespace OrderSphere.Mcp.Server.Tools;

// User-scoped basket tools. Read-only: the agent can inspect the current
// customer's cart to advise on it, but never mutates it. The cart is resolved
// for the authenticated caller via the forwarded bearer token.
[McpServerToolType]
public sealed class BasketTools
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [McpServerTool(Name = "get_my_cart")]
    [Description("View the current customer's shopping cart: line items with product name, unit price, quantity, and the cart total. Read-only.")]
    public static async Task<string> GetMyCartAsync(
        ICallerContext caller,
        IOrderSphereGateway gateway,
        CancellationToken ct = default)
    {
        if (!caller.HasBearerToken)
        {
            return UserToolGuard.AuthRequired;
        }

        var cart = await gateway.GetMyCartAsync(ct);
        if (cart is null)
        {
            return "No cart available. The caller may not be an authenticated customer.";
        }

        var items = cart.Items
            .Select(i => new
            {
                i.ProductName,
                i.Price,
                i.Quantity,
                LineTotal = i.Price * i.Quantity
            })
            .ToList();

        return JsonSerializer.Serialize(
            new { itemCount = items.Count, total = items.Sum(i => i.LineTotal), items },
            Json);
    }
}
