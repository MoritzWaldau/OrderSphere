using OrderSphere.Application.Abstraction;
using OrderSphere.Domain.Abstraction;
using OrderSphere.Domain.Primitives;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderSphere.Application.Features.Product.CreateProduct
{
    public sealed class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, Result>
    {
        public async Task<Result> Handle(CreateProductCommand request, CancellationToken cancellationToken)
        {
            
            await Task.FromResult(0);
            return Result.Success();
        }
    }
}
