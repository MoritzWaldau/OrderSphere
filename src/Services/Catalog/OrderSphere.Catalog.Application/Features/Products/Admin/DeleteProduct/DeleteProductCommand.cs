using OrderSphere.BuildingBlocks.StronglyTypedIds;

namespace OrderSphere.Catalog.Application.Features.Products.Admin.DeleteProduct;

public sealed record DeleteProductCommand(ProductId ProductId) : ICommand<Result>;
