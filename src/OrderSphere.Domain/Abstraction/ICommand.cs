using MediatR;

namespace OrderSphere.Domain.Abstraction;

public interface ICommand<out TResponse> : IRequest<TResponse>;
