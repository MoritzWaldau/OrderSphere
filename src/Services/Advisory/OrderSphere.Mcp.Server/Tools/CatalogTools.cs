using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using OrderSphere.Mcp.Server.Gateway;

namespace OrderSphere.Mcp.Server.Tools;

// Public catalog tools — no authentication required. Available to the internal
// advisory agent and to external MCP clients (anonymous browsing).
[McpServerToolType]
public sealed class CatalogTools
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [McpServerTool(Name = "search_products")]
    [Description("Search the product catalog by free-text query (matches name, description, or category). Returns matching products with price and stock.")]
    public static async Task<string> SearchProductsAsync(
        IOrderSphereGateway gateway,
        [Description("Search terms, e.g. 'running shoes' or 'red jacket'.")] string query,
        [Description("Maximum number of products to return (1-50). Defaults to 10.")] int maxResults = 10,
        CancellationToken ct = default)
    {
        var limit = Math.Clamp(maxResults, 1, 50);
        var page = await gateway.GetProductsAsync(1, 50, ct);

        var terms = (query ?? string.Empty)
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        var matches = page.Items
            .Where(p => p.IsActive)
            .Where(p => terms.Length == 0 || terms.All(t =>
                p.Name.Contains(t, StringComparison.OrdinalIgnoreCase) ||
                p.Description.Contains(t, StringComparison.OrdinalIgnoreCase) ||
                p.CategoryName.Contains(t, StringComparison.OrdinalIgnoreCase)))
            .Take(limit)
            .Select(p => new { p.Name, p.Slug, p.Price, p.Stock, Category = p.CategoryName, p.SKU })
            .ToList();

        return JsonSerializer.Serialize(new { count = matches.Count, products = matches }, Json);
    }

    [McpServerTool(Name = "get_product")]
    [Description("Get full details for a single product by its slug (URL identifier).")]
    public static async Task<string> GetProductAsync(
        IOrderSphereGateway gateway,
        [Description("The product slug, e.g. 'mens-trail-runner-x1'.")] string slug,
        CancellationToken ct = default)
    {
        var product = await gateway.GetProductBySlugAsync(slug, ct);
        return product is null
            ? $"No product found with slug '{slug}'."
            : JsonSerializer.Serialize(product, Json);
    }

    [McpServerTool(Name = "list_categories")]
    [Description("List the product categories available in the store, with the number of products in each.")]
    public static async Task<string> ListCategoriesAsync(
        IOrderSphereGateway gateway,
        CancellationToken ct = default)
    {
        var page = await gateway.GetCategoriesAsync(1, 50, ct);
        var categories = page.Items
            .Select(c => new { c.Name, c.Description, c.ProductCount });
        return JsonSerializer.Serialize(categories, Json);
    }
}
