using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Ordering.Application.Features.Coupon.Admin.DeactivateCoupon;

public sealed record DeactivateCouponCommand(Guid Id) : ICommand<Result>;
