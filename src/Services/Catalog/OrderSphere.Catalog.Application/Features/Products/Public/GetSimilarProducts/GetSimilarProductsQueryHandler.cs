using OrderSphere.BuildingBlocks.StronglyTypedIds;

namespace OrderSphere.Catalog.Application.Features.Products.Public.GetSimilarProducts;

public sealed class GetSimilarProductsQueryHandler(
    ICatalogDbContext context,
    IProductSearchIndex searchIndex,
    IBlobStorageService blobService)
    : IQueryHandler<GetSimilarProductsQuery, Result<IReadOnlyList<ProductDto>>>
{
    public async Task<Result<IReadOnlyList<ProductDto>>> Handle(GetSimilarProductsQuery request, CancellationToken ct)
    {
        var source = await context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .AsNoTracking()
            .Where(p => p.Slug == request.Slug && p.IsActive)
            .Select(p => new
            {
                Id = p.Id.Value,
                p.Name,
                CategoryName = p.Category != null ? p.Category.Name : string.Empty,
                BrandName = p.Brand != null ? p.Brand.Name : string.Empty,
                p.Description,
            })
            .FirstOrDefaultAsync(ct);

        if (source is null)
            return Result<IReadOnlyList<ProductDto>>.Failure(ProductErrors.NotFound);

        var limit = Math.Clamp(request.Limit, 1, 20);

        if (!searchIndex.IsEnabled)
            return Result<IReadOnlyList<ProductDto>>.Success([]);

        var productText = $"{source.Name}\n{source.BrandName}\n{source.CategoryName}\n{source.Description}";
        var similarIds = await searchIndex.FindSimilarAsync(productText, source.Id, limit, ct);

        if (similarIds.Count == 0)
            return Result<IReadOnlyList<ProductDto>>.Success([]);

        return await FetchByIdsAsync(similarIds, ct);
    }

    private async Task<Result<IReadOnlyList<ProductDto>>> FetchByIdsAsync(
        IReadOnlyList<Guid> ids, CancellationToken ct)
    {
        var typedIds = ids.Select(ProductId.From).ToList();

        var rawProducts = await context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .AsNoTracking()
            .Where(p => typedIds.Contains(p.Id) && p.IsActive)
            .Select(p => new
            {
                Id = p.Id.Value,
                p.Name,
                p.Slug,
                p.Description,
                Price = (decimal)p.Price,
                p.Stock,
                CategoryId = p.CategoryId.Value,
                CategoryName = p.Category!.Name,
                p.SKU,
                p.ImageUrl,
                p.ImageBlobName,
                p.IsActive,
                p.AverageRating,
                p.ReviewCount,
                BrandId = p.Brand != null ? (Guid?)p.Brand.Id.Value : null,
                BrandName = p.Brand != null ? p.Brand.Name : null,
            })
            .ToListAsync(ct);

        var products = new List<ProductDto>(rawProducts.Count);
        foreach (var raw in rawProducts)
        {
            var imageUrl = raw.ImageBlobName is not null && blobService.IsEnabled
                ? await blobService.GetSasUrlAsync(raw.ImageBlobName, ct)
                : raw.ImageUrl;
            products.Add(new ProductDto(raw.Id, raw.Name, raw.Slug, raw.Description, raw.Price,
                raw.Stock, raw.CategoryId, raw.CategoryName, raw.SKU, imageUrl,
                raw.IsActive, raw.AverageRating, raw.ReviewCount, raw.BrandId, raw.BrandName));
        }

        // Preserve relevance order from the search index.
        var byId = products.ToDictionary(p => p.Id);
        var ordered = ids.Where(byId.ContainsKey).Select(id => byId[id]).ToList();

        return Result<IReadOnlyList<ProductDto>>.Success(ordered);
    }
}
