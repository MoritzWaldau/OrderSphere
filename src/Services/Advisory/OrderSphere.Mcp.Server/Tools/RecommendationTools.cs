using System.ComponentModel;
using System.Text.Json;
using ModelContextProtocol.Server;
using OrderSphere.Mcp.Server.Gateway;

namespace OrderSphere.Mcp.Server.Tools;

[McpServerToolType]
public sealed class RecommendationTools
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    [McpServerTool(Name = "get_similar_products", Title = "Get similar products",
        ReadOnly = true, Idempotent = true, Destructive = false, OpenWorld = false)]
    [Description(
        "Returns products that are semantically similar to the given product slug, " +
        "based on vector similarity of name, category, brand, and description. " +
        "Use after get_product or search_products to suggest related items or cross-sells. " +
        "Returns an empty list when the search index is not configured.")]
    public static async Task<string> GetSimilarProductsAsync(
        IOrderSphereGateway gateway,
        [Description("Product slug to base recommendations on. Use search_products to find slugs.")] string slug,
        [Description("Maximum number of similar products to return (1–10). Defaults to 5.")] int limit = 5,
        CancellationToken ct = default)
    {
        var clampedLimit = Math.Clamp(limit, 1, 10);
        var products = await gateway.GetSimilarProductsAsync(slug, clampedLimit, ct);

        if (products.Count == 0)
            return JsonSerializer.Serialize(new { results = Array.Empty<object>(), count = 0 }, Json);

        var results = products.Select(p => new
        {
            slug = p.Slug,
            name = p.Name,
            price = p.Price,
            categoryName = p.CategoryName,
            isActive = p.IsActive,
            stock = p.Stock,
        });

        return JsonSerializer.Serialize(new { results, count = products.Count }, Json);
    }
}
