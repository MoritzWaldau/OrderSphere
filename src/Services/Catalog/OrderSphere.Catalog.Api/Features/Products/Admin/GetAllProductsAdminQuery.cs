using MediatR;
using OrderSphere.Catalog.Api.Models.Admin;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Catalog.Api.Features.Products.Admin;

public sealed record GetAllProductsAdminQuery : IRequest<Result<IEnumerable<AdminProductDto>>>;
