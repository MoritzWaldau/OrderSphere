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

    [McpServerTool(Name = "search_products", Title = "Search products",
        ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description("Search the product catalog by free-text query (matches name or description), optionally filtered by category and price range. Returns matching products with price and stock.")]
    public static async Task<string> SearchProductsAsync(
        IOrderSphereGateway gateway,
        [Description("Search terms, e.g. 'running shoes' or 'red jacket'. May be empty to list products filtered only by category/price.")] string query,
        [Description("Optional exact category name to filter by, e.g. 'Shoes'. Use list_categories to discover names.")] string? category = null,
        [Description("Optional minimum price (inclusive).")] decimal? minPrice = null,
        [Description("Optional maximum price (inclusive).")] decimal? maxPrice = null,
        [Description("Maximum number of products to return (1-50). Defaults to 10.")] int maxResults = 10,
        CancellationToken ct = default)
    {
        var limit = Math.Clamp(maxResults, 1, 50);

        // Filtering happens server-side in the Catalog service; this tool only shapes
        // the result for the model.
        var page = await gateway.GetProductsAsync(
            1, limit,
            string.IsNullOrWhiteSpace(query) ? null : query.Trim(),
            category, minPrice, maxPrice, ct);

        var matches = page.Items
            .Select(p => new { p.Name, p.Slug, p.Price, p.Stock, Category = p.CategoryName, p.SKU })
            .ToList();

        return JsonSerializer.Serialize(
            new { count = matches.Count, totalMatches = page.TotalCount, products = matches }, Json);
    }

    [McpServerTool(Name = "get_product", Title = "Get product details",
        ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
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

    [McpServerTool(Name = "list_categories", Title = "List categories",
        ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
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
