using OrderSphere.Application.Models;
using OrderSphere.Application.Models.Admin;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Abstraction;

public interface ICatalogClient
{
    // Product queries
    Task<Result<IEnumerable<ProductDto>>> GetProductsAsync(CancellationToken ct = default);
    Task<Result<ProductDto>> GetProductBySlugAsync(string slug, CancellationToken ct = default);
    Task<Result<CatalogProductInfo>> GetProductByIdAsync(Guid productId, CancellationToken ct = default);
    Task<Result<IReadOnlyDictionary<Guid, string>>> GetProductNamesByIdsAsync(IEnumerable<Guid> productIds, CancellationToken ct = default);

    // Product admin
    Task<Result<IEnumerable<AdminProductDto>>> GetAllProductsAdminAsync(CancellationToken ct = default);
    Task<Result<AdminProductDto>> GetProductByIdAdminAsync(Guid productId, CancellationToken ct = default);
    Task<Result<Guid>> CreateProductAsync(AdminProductInput input, CancellationToken ct = default);
    Task<Result<bool>> UpdateProductAsync(Guid productId, AdminProductInput input, CancellationToken ct = default);
    Task<Result<bool>> DeleteProductAsync(Guid productId, CancellationToken ct = default);

    // Category queries
    Task<Result<IEnumerable<CategoryDto>>> GetCategoriesAsync(CancellationToken ct = default);

    // Category admin
    Task<Result<IEnumerable<AdminCategoryDto>>> GetAllCategoriesAdminAsync(CancellationToken ct = default);
    Task<Result<Guid>> CreateCategoryAsync(string name, string description, CancellationToken ct = default);
    Task<Result<bool>> UpdateCategoryAsync(Guid categoryId, string name, string description, bool isActive, CancellationToken ct = default);
    Task<Result<bool>> DeleteCategoryAsync(Guid categoryId, CancellationToken ct = default);

    // Stock operations (called by Cart/Checkout/CancelOrder)
    Task<Result> DecrementStockAsync(Guid productId, int quantity, CancellationToken ct = default);
    Task<Result> RestoreStockAsync(Guid productId, int quantity, CancellationToken ct = default);
}

public sealed record CatalogProductInfo(Guid Id, string Name, decimal Price, int Stock, bool IsActive);
