using MediatR;

namespace OrderSphere.BuildingBlocks.Abstraction;

public interface IQuery<out TResponse> : IRequest<TResponse>
    where TResponse : notnull;
