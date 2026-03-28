using MediatR;
using Microsoft.AspNetCore.Components;
using OrderSphere.Application.Features.Product.GetProduct;
using OrderSphere.Application.Models;
using OrderSphere.Domain.Extensions;

namespace OrderSphere.UI.Client.Pages;


public partial class Products
{
    [Inject]
    public required ISender Sender { get; set; }

    private IEnumerable<ProductDto> products = [];

    protected override async Task OnInitializedAsync()
    {
        var result = await Sender.Send(new GetProductQuery());

        result.Match(
           onSuccess: products => this.products = products,
           onFailure: error => Snackbar.Add(error.Description, MudBlazor.Severity.Error)
        );
    }

}
