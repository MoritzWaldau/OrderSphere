using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using OrderSphere.Mcp.Server.Gateway;

namespace OrderSphere.Mcp.Server.Tools;

// User-scoped profile tools. Resolves the profile of the authenticated caller
// via the forwarded bearer token.
[McpServerToolType]
public sealed class ProfileTools
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [McpServerTool(Name = "get_my_profile", Title = "Get my profile",
        ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Get the current customer's profile: display name, email, preferences, and saved addresses.")]
    public static async Task<string> GetMyProfileAsync(
        ICallerContext caller,
        IOrderSphereGateway gateway,
        CancellationToken ct = default)
    {
        if (!caller.HasBearerToken)
        {
            return UserToolGuard.AuthRequired;
        }

        var profile = await gateway.GetMyProfileAsync(ct);
        if (profile is null)
        {
            return "No profile available. The caller may not be an authenticated customer.";
        }

        var result = new
        {
            profile.DisplayName,
            profile.Email,
            profile.DarkModeEnabled,
            Addresses = profile.Addresses.Select(a => new
            {
                a.Label,
                a.FirstName,
                a.LastName,
                a.Street,
                a.City,
                a.PostalCode,
                a.Country,
                a.IsDefault
            })
        };
        return JsonSerializer.Serialize(result, Json);
    }

    [McpServerTool(Name = "list_my_addresses", Title = "List my addresses",
        ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("List the current customer's saved shipping addresses, including which one is the default.")]
    public static async Task<string> ListMyAddressesAsync(
        ICallerContext caller,
        IOrderSphereGateway gateway,
        CancellationToken ct = default)
    {
        if (!caller.HasBearerToken)
        {
            return UserToolGuard.AuthRequired;
        }

        var addresses = await gateway.GetMyAddressesAsync(ct);
        if (addresses.Count == 0)
        {
            return "No saved addresses available. The caller may not be an authenticated customer, or has none on file.";
        }

        var result = addresses.Select(a => new
        {
            a.Label,
            a.FirstName,
            a.LastName,
            a.Street,
            a.City,
            a.PostalCode,
            a.Country,
            a.IsDefault
        });
        return JsonSerializer.Serialize(new { count = addresses.Count, addresses = result }, Json);
    }
}
