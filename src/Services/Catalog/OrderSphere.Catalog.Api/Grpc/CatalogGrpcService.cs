using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using OrderSphere.Catalog.Infrastructure.Persistence;
using OrderSphere.Catalog.V1;

namespace OrderSphere.Catalog.Api.Grpc;

public sealed class CatalogGrpcService(CatalogDbContext context) : CatalogService.CatalogServiceBase
{
    public override async Task<GetProductResponse> GetProduct(GetProductRequest request, ServerCallContext ctx)
    {
        if (!Guid.TryParse(request.ProductId, out var id))
            return new GetProductResponse { Found = false };

        var p = await context.Products
            .Include(x => x.Category)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ctx.CancellationToken);

        return p is null ? new GetProductResponse { Found = false } : MapProduct(p);
    }

    public override async Task<GetProductResponse> GetProductBySlug(GetProductBySlugRequest request, ServerCallContext ctx)
    {
        var p = await context.Products
            .Include(x => x.Category)
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Slug == request.Slug && !x.IsDeleted, ctx.CancellationToken);

        return p is null ? new GetProductResponse { Found = false } : MapProduct(p);
    }

    public override async Task<CheckStockResponse> CheckStock(CheckStockRequest request, ServerCallContext ctx)
    {
        if (!Guid.TryParse(request.ProductId, out var id))
            return new CheckStockResponse { IsAvailable = false, CurrentStock = 0 };

        var stock = await context.Products
            .AsNoTracking()
            .Where(p => p.Id == id && !p.IsDeleted)
            .Select(p => (int?)p.Stock)
            .FirstOrDefaultAsync(ctx.CancellationToken);

        return stock is null
            ? new CheckStockResponse { IsAvailable = false, CurrentStock = 0 }
            : new CheckStockResponse { IsAvailable = stock >= request.Quantity, CurrentStock = stock.Value };
    }

    public override async Task<StockOperationResponse> DecrementStock(DecrementStockRequest request, ServerCallContext ctx)
    {
        if (!Guid.TryParse(request.ProductId, out var id))
            return Fail("PRODUCT_NOT_FOUND", "Product not found.");

        var product = await context.Products
            .AsTracking()
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ctx.CancellationToken);

        if (product is null) return Fail("PRODUCT_NOT_FOUND", "Product not found.");
        if (product.Stock < request.Quantity) return Fail("INSUFFICIENT_STOCK", "Insufficient stock.");

        product.RemoveFromStock(request.Quantity);
        await context.SaveChangesAsync(ctx.CancellationToken);
        return new StockOperationResponse { Success = true };
    }

    public override async Task<StockOperationResponse> RestoreStock(RestoreStockRequest request, ServerCallContext ctx)
    {
        if (!Guid.TryParse(request.ProductId, out var id))
            return Fail("PRODUCT_NOT_FOUND", "Product not found.");

        var product = await context.Products
            .AsTracking()
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted, ctx.CancellationToken);

        if (product is null) return Fail("PRODUCT_NOT_FOUND", "Product not found.");

        product.AddToStock(request.Quantity);
        await context.SaveChangesAsync(ctx.CancellationToken);
        return new StockOperationResponse { Success = true };
    }

    public override async Task<GetProductNamesResponse> GetProductNames(GetProductNamesRequest request, ServerCallContext ctx)
    {
        var ids = request.ProductIds
            .Select(s => Guid.TryParse(s, out var g) ? (Guid?)g : null)
            .Where(g => g.HasValue)
            .Select(g => g!.Value)
            .ToList();

        var names = await context.Products
            .AsNoTracking()
            .Where(p => ids.Contains(p.Id) && !p.IsDeleted)
            .Select(p => new { p.Id, p.Name })
            .ToListAsync(ctx.CancellationToken);

        var response = new GetProductNamesResponse();
        foreach (var n in names)
            response.Names[n.Id.ToString()] = n.Name;

        return response;
    }

    private static GetProductResponse MapProduct(Domain.Entities.Product p) => new()
    {
        Found = true,
        Id = p.Id.ToString(),
        Name = p.Name,
        Slug = p.Slug,
        Description = p.Description,
        Price = (double)p.Price,
        Stock = p.Stock,
        CategoryId = p.CategoryId.ToString(),
        CategoryName = p.Category?.Name ?? string.Empty,
        Sku = p.SKU,
        ImageUrl = p.ImageUrl ?? string.Empty,
        IsActive = p.IsActive,
    };

    private static StockOperationResponse Fail(string code, string description)
        => new() { Success = false, ErrorCode = code, ErrorDescription = description };
}
