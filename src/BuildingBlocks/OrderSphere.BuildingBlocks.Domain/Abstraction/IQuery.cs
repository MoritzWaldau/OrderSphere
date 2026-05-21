using MediatR;

namespace OrderSphere.Domain.Abstraction;

public interface IQuery<out TResponse> : IRequest<TResponse>
    where TResponse : notnull;
