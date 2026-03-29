using MediatR;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using OrderSphere.Application.Features.Product.GetProduct;
using OrderSphere.Application.Models;
using OrderSphere.Domain.Extensions;
using OrderSphere.UI.Configuration;

namespace OrderSphere.UI.Components.Pages;

public partial class Products : OrderSphereComponentBase
{
    

    private IEnumerable<ProductDto> _products = [];

    protected override async Task LoadDataAsync()
    {
        var result = await Sender.Send(new GetProductQuery());

        result.Match(
            onSuccess: products => _products = products,
            onFailure: error => Snackbar.Add(error.Description, Severity.Error)
        );
    }
}
