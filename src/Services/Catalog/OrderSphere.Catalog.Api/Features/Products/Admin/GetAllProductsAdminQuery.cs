using MediatR;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.Catalog.Api.Models.Admin;

namespace OrderSphere.Catalog.Api.Features.Products.Admin;

public sealed record GetAllProductsAdminQuery : IRequest<Result<IEnumerable<AdminProductDto>>>;
