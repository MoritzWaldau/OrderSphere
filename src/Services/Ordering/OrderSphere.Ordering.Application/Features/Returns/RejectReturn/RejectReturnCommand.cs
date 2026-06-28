using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Ordering.Application.Features.Returns.RejectReturn;

public sealed record RejectReturnCommand(Guid ReturnRequestId, string? Note) : ICommand<Result>;
