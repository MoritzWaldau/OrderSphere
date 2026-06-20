using OrderSphere.BuildingBlocks.StronglyTypedIds;

namespace OrderSphere.Catalog.Application.Features.Products.Public.GetProducts;

public sealed class GetProductsQueryHandler(
    ICatalogDbContext context,
    IProductSearchIndex searchIndex,
    IBlobStorageService blobService)
    : IQueryHandler<GetProductsQuery, Result<PagedResult<ProductDto>>>
{
    public async Task<Result<PagedResult<ProductDto>>> Handle(GetProductsQuery request, CancellationToken ct)
    {
        // Hybrid (keyword + vector) search applies only when there is a free-text term
        // and the index is configured. Everything else (browsing, filtering, sorting)
        // stays on the database, which remains the system of record.
        if (searchIndex.IsEnabled && !string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            try
            {
                return await SearchViaIndexAsync(request, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // A search outage must not take down browsing — fall back to the database.
                _ = ex;
            }
        }

        return await SearchViaDatabaseAsync(request, ct);
    }

    private async Task<Result<PagedResult<ProductDto>>> SearchViaIndexAsync(GetProductsQuery request, CancellationToken ct)
    {
        var criteria = new ProductSearchCriteria(
            request.SearchTerm!.Trim(),
            string.IsNullOrWhiteSpace(request.CategoryName) ? null : request.CategoryName.Trim(),
            request.MinPrice,
            request.MaxPrice,
            (request.Page - 1) * request.PageSize,
            request.PageSize);

        var page = await searchIndex.SearchAsync(criteria, ct);

        var total = (int)page.Total;

        if (page.ProductIds.Count == 0)
            return Result<PagedResult<ProductDto>>.Success(
                new PagedResult<ProductDto>([], total, request.Page, request.PageSize));

        var typedIds = page.ProductIds.Select(ProductId.From).ToList();

        var rawProducts = await context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .AsNoTracking()
            .Where(p => typedIds.Contains(p.Id))
            .Select(p => new
            {
                Id = p.Id.Value,
                p.Name, p.Slug, p.Description,
                Price = (decimal)p.Price,
                p.Stock,
                CategoryId = p.CategoryId.Value,
                CategoryName = p.Category!.Name,
                p.SKU, p.ImageUrl, p.ImageBlobName, p.IsActive, p.AverageRating, p.ReviewCount,
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

        // Preserve the relevance order returned by the search index.
        var byId = products.ToDictionary(p => p.Id);
        var ordered = page.ProductIds
            .Where(byId.ContainsKey)
            .Select(id => byId[id])
            .ToList();

        return Result<PagedResult<ProductDto>>.Success(
            new PagedResult<ProductDto>(ordered, total, request.Page, request.PageSize));
    }

    private async Task<Result<PagedResult<ProductDto>>> SearchViaDatabaseAsync(GetProductsQuery request, CancellationToken ct)
    {
        var query = context.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .AsNoTracking()
            .Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var term = request.SearchTerm.Trim().ToLower();
            query = query.Where(p =>
                p.Name.ToLower().Contains(term) ||
                p.Description.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.CategoryName))
        {
            var category = request.CategoryName.Trim().ToLower();
            query = query.Where(p => p.Category!.Name.ToLower() == category);
        }

        if (request.MinPrice is { } minPrice)
        {
            query = query.Where(p => p.Price.Amount >= minPrice);
        }

        if (request.MaxPrice is { } maxPrice)
        {
            query = query.Where(p => p.Price.Amount <= maxPrice);
        }

        var total = await query.CountAsync(ct);

        var ordered = (request.SortBy, request.SortDir) switch
        {
            (ProductSortBy.Price, SortDirection.Desc) => query.OrderByDescending(p => p.Price.Amount),
            (ProductSortBy.Price, _) => query.OrderBy(p => p.Price.Amount),
            (ProductSortBy.Newest, SortDirection.Asc) => query.OrderBy(p => p.CreatedAt),
            (ProductSortBy.Newest, _) => query.OrderByDescending(p => p.CreatedAt),
            (_, SortDirection.Desc) => query.OrderByDescending(p => p.Name),
            _ => query.OrderBy(p => p.Name),
        };

        var rawItems = await ordered
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(p => new
            {
                Id = p.Id.Value,
                p.Name, p.Slug, p.Description,
                Price = (decimal)p.Price,
                p.Stock,
                CategoryId = p.CategoryId.Value,
                CategoryName = p.Category!.Name,
                p.SKU, p.ImageUrl, p.ImageBlobName, p.IsActive, p.AverageRating, p.ReviewCount,
                BrandId = p.Brand != null ? (Guid?)p.Brand.Id.Value : null,
                BrandName = p.Brand != null ? p.Brand.Name : null,
            })
            .ToListAsync(ct);

        var items = new List<ProductDto>(rawItems.Count);
        foreach (var raw in rawItems)
        {
            var imageUrl = raw.ImageBlobName is not null && blobService.IsEnabled
                ? await blobService.GetSasUrlAsync(raw.ImageBlobName, ct)
                : raw.ImageUrl;
            items.Add(new ProductDto(raw.Id, raw.Name, raw.Slug, raw.Description, raw.Price,
                raw.Stock, raw.CategoryId, raw.CategoryName, raw.SKU, imageUrl,
                raw.IsActive, raw.AverageRating, raw.ReviewCount, raw.BrandId, raw.BrandName));
        }

        return Result<PagedResult<ProductDto>>.Success(
            new PagedResult<ProductDto>(items, total, request.Page, request.PageSize));
    }
}
