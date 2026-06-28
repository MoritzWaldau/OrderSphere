using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Ordering.Application.Features.Returns.ApproveReturn;

public sealed record ApproveReturnCommand(Guid ReturnRequestId, string? Note) : ICommand<Result>;
