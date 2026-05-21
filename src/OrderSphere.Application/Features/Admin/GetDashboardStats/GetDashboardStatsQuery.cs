using OrderSphere.Application.Models.Admin;
using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Application.Features.Admin.GetDashboardStats;

public sealed record GetDashboardStatsQuery
    : IQuery<Result<AdminDashboardDto>>;
