using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using OrderSphere.Mcp.Server.Gateway;

namespace OrderSphere.Mcp.Server.Tools;

// Write-action tool: adds a product to the current customer's cart.
// Two-phase: confirmed=false returns a confirmation_required payload (no mutation);
// confirmed=true executes the POST only after the customer has explicitly approved.
[McpServerToolType]
public sealed class BasketWriteTools
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [McpServerTool(Name = "add_to_cart", Title = "Add item to cart",
        ReadOnly = false, Idempotent = false, Destructive = false, OpenWorld = false)]
    [Description(
        "Add a product to the current customer's shopping cart. " +
        "Always call with confirmed=false first; the tool returns a confirmation_required JSON payload. " +
        "Call again with confirmed=true only after the customer has explicitly approved.")]
    public static async Task<string> AddToCartAsync(
        ICallerContext caller,
        IOrderSphereGateway gateway,
        [Description("Product slug from the catalog. Use search_products to find the slug.")] string slug,
        [Description("Number of units to add. Must be at least 1.")] int quantity,
        [Description("Set to true only after explicit customer approval. Always start with false.")] bool confirmed = false,
        CancellationToken ct = default)
    {
        if (!caller.HasBearerToken)
            return UserToolGuard.AuthRequired;

        if (quantity < 1)
            return JsonSerializer.Serialize(new { error = "Menge muss mindestens 1 sein." }, Json);

        var product = await gateway.GetProductBySlugAsync(slug, ct);
        if (product is null)
            return JsonSerializer.Serialize(new { error = $"Produkt '{slug}' nicht gefunden." }, Json);

        if (!product.IsActive || product.Stock < 1)
            return JsonSerializer.Serialize(new { error = $"'{product.Name}' ist derzeit nicht verfügbar." }, Json);

        if (!confirmed)
        {
            var total = product.Price * quantity;
            return JsonSerializer.Serialize(new
            {
                __confirm__ = "add_to_cart",
                slug = product.Slug,
                quantity,
                productName = product.Name,
                unitPrice = product.Price,
                summary = $"{product.Name} ({quantity}×) für {total:N2} €"
            }, Json);
        }

        var result = await gateway.AddToCartAsync(product.Id, quantity, ct);
        if (!result.Success)
        {
            return result.Error switch
            {
                "insufficient_stock" => JsonSerializer.Serialize(
                    new { error = $"Nicht genug Lagerbestand für '{product.Name}'." }, Json),
                "product_not_found" => JsonSerializer.Serialize(
                    new { error = $"'{product.Name}' ist nicht mehr verfügbar." }, Json),
                _ => JsonSerializer.Serialize(
                    new { error = "Der Artikel konnte nicht in den Warenkorb gelegt werden." }, Json)
            };
        }

        return JsonSerializer.Serialize(new
        {
            success = true,
            message = $"{product.Name} ({quantity}×) wurde erfolgreich in den Warenkorb gelegt."
        }, Json);
    }
}
