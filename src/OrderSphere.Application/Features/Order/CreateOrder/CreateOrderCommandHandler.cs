using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Primitives;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderSphere.Application.Features.Order.CreateOrder
{
    public sealed class CreateOrderCommandHandler : ICommandHandler<CreateOrderCommand, Result>
    {
        public async Task<Result> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
