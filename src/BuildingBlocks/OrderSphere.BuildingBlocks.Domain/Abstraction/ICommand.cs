using MediatR;

namespace OrderSphere.BuildingBlocks.Abstraction;

public interface ICommand<out TResponse> : IRequest<TResponse>;
