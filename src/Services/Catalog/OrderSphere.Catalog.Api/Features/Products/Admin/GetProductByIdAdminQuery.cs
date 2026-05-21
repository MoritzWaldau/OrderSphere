using MediatR;
using OrderSphere.Catalog.Api.Models.Admin;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Catalog.Api.Features.Products.Admin;

public sealed record GetProductByIdAdminQuery(Guid ProductId) : IRequest<Result<AdminProductDto>>;
