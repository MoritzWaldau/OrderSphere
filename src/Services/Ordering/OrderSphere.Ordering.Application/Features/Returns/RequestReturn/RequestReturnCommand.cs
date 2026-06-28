using OrderSphere.BuildingBlocks.Abstraction;
using OrderSphere.BuildingBlocks.Primitives;

namespace OrderSphere.Ordering.Application.Features.Returns.RequestReturn;

public sealed record RequestReturnCommand(
    Guid OrderId,
    Guid CustomerId,
    string Reason,
    IReadOnlyList<RequestReturnLine> Items) : ICommand<Result<Guid>>;

public sealed record RequestReturnLine(Guid ProductId, int Quantity);
