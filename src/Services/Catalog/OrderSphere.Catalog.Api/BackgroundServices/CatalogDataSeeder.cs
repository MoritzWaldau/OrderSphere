using System.Reflection;
using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Catalog.Application.Abstractions;
using OrderSphere.Catalog.Application.Features.Brands.Admin.CreateBrand;
using OrderSphere.Catalog.Application.Features.Categories.Admin.CreateCategory;
using OrderSphere.Catalog.Application.Features.Products.Admin.CreateProduct;
using OrderSphere.Catalog.Application.Features.Products.Admin.UploadProductImage;

namespace OrderSphere.Catalog.Api.BackgroundServices;

/// <summary>
/// On startup (Development by default, or when <c>Catalog:SeedData</c> is true),
/// loads an embedded product catalogue and creates any brands, categories and
/// products that do not yet exist. Idempotent: existing rows (matched by name or
/// SKU) are left untouched, so repeated restarts never duplicate data.
/// </summary>
/// <remarks>
/// Seeding reuses the existing MediatR commands rather than touching the database
/// directly, so validation, slug/Money construction and search-index sync all run
/// exactly as they would for an admin API call. Image upload is best-effort: when
/// blob storage is configured each product image is downloaded from its source URL
/// and stored in the blob container; otherwise the source URL is kept as the
/// product's external <c>ImageUrl</c>.
/// </remarks>
public sealed class CatalogDataSeeder(
    IServiceScopeFactory scopeFactory,
    IHostEnvironment environment,
    IConfiguration configuration,
    IHttpClientFactory httpClientFactory,
    ILogger<CatalogDataSeeder> logger) : BackgroundService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var enabled = configuration.GetValue<bool?>("Catalog:SeedData") ?? environment.IsDevelopment();
        if (!enabled)
            return;

        try
        {
            var data = LoadSeedData();
            if (data is null)
            {
                logger.LogWarning("Catalog seed data resource not found; skipping seeding.");
                return;
            }

            await using var scope = scopeFactory.CreateAsyncScope();
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();
            var context = scope.ServiceProvider.GetRequiredService<ICatalogDbContext>();
            var blobService = scope.ServiceProvider.GetRequiredService<IBlobStorageService>();

            var brandIds = await SeedBrandsAsync(sender, context, data.Brands, stoppingToken);
            var categoryIds = await SeedCategoriesAsync(sender, context, data.Categories, stoppingToken);
            await SeedProductsAsync(sender, context, blobService, data.Products, brandIds, categoryIds, stoppingToken);

            logger.LogInformation("Catalog data seeding completed.");
        }
        catch (OperationCanceledException)
        {
            // Shutting down.
        }
        catch (Exception ex)
        {
            // Seeding is a convenience for dev/demo environments — never fatal.
            logger.LogError(ex, "Catalog data seeding failed.");
        }
    }

    private async Task<Dictionary<string, Guid>> SeedBrandsAsync(
        ISender sender, ICatalogDbContext context, IReadOnlyList<SeedBrand> brands, CancellationToken ct)
    {
        var existing = await context.Brands.AsNoTracking()
            .ToDictionaryAsync(b => b.Name, b => b.Id.Value, StringComparer.OrdinalIgnoreCase, ct);

        foreach (var brand in brands)
        {
            if (existing.ContainsKey(brand.Name))
                continue;

            var result = await sender.Send(new CreateBrandCommand(brand.Name, brand.Description ?? "", brand.LogoUrl), ct);
            if (result.IsSuccess)
                existing[brand.Name] = result.Value;
            else
                logger.LogWarning("Failed to seed brand '{Brand}': {Error}", brand.Name, result.Error.Code);
        }

        return existing;
    }

    private async Task<Dictionary<string, Guid>> SeedCategoriesAsync(
        ISender sender, ICatalogDbContext context, IReadOnlyList<SeedCategory> categories, CancellationToken ct)
    {
        var existing = await context.Categories.AsNoTracking()
            .ToDictionaryAsync(c => c.Name, c => c.Id.Value, StringComparer.OrdinalIgnoreCase, ct);

        foreach (var category in categories)
        {
            if (existing.ContainsKey(category.Name))
                continue;

            var result = await sender.Send(new CreateCategoryCommand(category.Name, category.Description ?? ""), ct);
            if (result.IsSuccess)
                existing[category.Name] = result.Value;
            else
                logger.LogWarning("Failed to seed category '{Category}': {Error}", category.Name, result.Error.Code);
        }

        return existing;
    }

    private async Task SeedProductsAsync(
        ISender sender,
        ICatalogDbContext context,
        IBlobStorageService blobService,
        IReadOnlyList<SeedProduct> products,
        IReadOnlyDictionary<string, Guid> brandIds,
        IReadOnlyDictionary<string, Guid> categoryIds,
        CancellationToken ct)
    {
        var existingSkus = await context.Products.AsNoTracking()
            .Select(p => p.SKU)
            .ToListAsync(ct);
        var skuSet = new HashSet<string>(existingSkus, StringComparer.OrdinalIgnoreCase);

        foreach (var product in products)
        {
            if (skuSet.Contains(product.Sku))
                continue;

            if (!categoryIds.TryGetValue(product.Category, out var categoryId))
            {
                logger.LogWarning("Skipping product '{Product}': unknown category '{Category}'.", product.Name, product.Category);
                continue;
            }

            BrandId? brandId = null;
            if (!string.IsNullOrWhiteSpace(product.Brand))
            {
                if (brandIds.TryGetValue(product.Brand, out var resolved))
                    brandId = BrandId.From(resolved);
                else
                    logger.LogWarning("Product '{Product}' references unknown brand '{Brand}'; creating without brand.", product.Name, product.Brand);
            }

            var createResult = await sender.Send(
                new CreateProductCommand(
                    product.Name,
                    product.Description ?? "",
                    product.Price,
                    product.Stock,
                    CategoryId.From(categoryId),
                    product.Sku,
                    product.ImageUrl,
                    brandId),
                ct);

            if (createResult.IsFailure)
            {
                logger.LogWarning("Failed to seed product '{Product}': {Error}", product.Name, createResult.Error.Code);
                continue;
            }

            skuSet.Add(product.Sku);

            if (blobService.IsEnabled && !string.IsNullOrWhiteSpace(product.ImageUrl))
                await TryUploadImageAsync(sender, createResult.Value, product.ImageUrl, ct);
        }
    }

    private async Task TryUploadImageAsync(ISender sender, Guid productId, string imageUrl, CancellationToken ct)
    {
        try
        {
            var http = httpClientFactory.CreateClient();
            http.Timeout = TimeSpan.FromSeconds(20);

            using var response = await http.GetAsync(imageUrl, ct);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("Image download for product {ProductId} returned {Status}.", productId, response.StatusCode);
                return;
            }

            var contentType = NormalizeContentType(response.Content.Headers.ContentType?.MediaType);
            if (contentType is null)
            {
                logger.LogWarning("Image for product {ProductId} has unsupported content type.", productId);
                return;
            }

            await using var source = await response.Content.ReadAsStreamAsync(ct);
            using var buffer = new MemoryStream();
            await source.CopyToAsync(buffer, ct);
            buffer.Position = 0;

            var fileName = $"seed{ExtensionFor(contentType)}";
            var result = await sender.Send(
                new UploadProductImageCommand(ProductId.From(productId), buffer, contentType, fileName), ct);

            if (result.IsFailure)
                logger.LogWarning("Image upload for product {ProductId} failed: {Error}", productId, result.Error.Code);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogWarning(ex, "Image upload for product {ProductId} failed.", productId);
        }
    }

    private static string? NormalizeContentType(string? mediaType) => mediaType?.ToLowerInvariant() switch
    {
        "image/jpeg" or "image/jpg" => "image/jpeg",
        "image/png" => "image/png",
        "image/webp" => "image/webp",
        "image/gif" => "image/gif",
        _ => null,
    };

    private static string ExtensionFor(string contentType) => contentType switch
    {
        "image/png" => ".png",
        "image/webp" => ".webp",
        "image/gif" => ".gif",
        _ => ".jpg",
    };

    private static SeedData? LoadSeedData()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = Array.Find(
            assembly.GetManifestResourceNames(),
            n => n.EndsWith("products.json", StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
            return null;

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        return JsonSerializer.Deserialize<SeedData>(stream, JsonOptions);
    }

    private sealed record SeedData(
        IReadOnlyList<SeedBrand> Brands,
        IReadOnlyList<SeedCategory> Categories,
        IReadOnlyList<SeedProduct> Products);

    private sealed record SeedBrand(string Name, string? Description, string? LogoUrl);

    private sealed record SeedCategory(string Name, string? Description);

    private sealed record SeedProduct(
        string Name,
        string? Brand,
        string Category,
        string? Description,
        decimal Price,
        int Stock,
        string Sku,
        string? ImageUrl);
}
