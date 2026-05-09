using OrderSphere.Application.Models.Admin;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Primitives;

namespace OrderSphere.Application.Features.Admin.GetDashboardStats;

public sealed record GetDashboardStatsQuery
    : IQuery<Result<AdminDashboardDto>>;
