using MediatR;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using OrderSphere.Application.Features.Product.GetProduct;
using OrderSphere.Application.Models;
using OrderSphere.Domain.Extensions;

namespace OrderSphere.UI.Components.Pages;

public partial class Products
{
    [Inject] 
    public required ISender Sender { get; set; }

    private IEnumerable<ProductDto> _products = [];
    private bool _loading = true;
    private bool _firstRender = true;

    protected override async Task OnInitializedAsync()
    {
        // Beim PreRender nichts laden, Skeleton zeigen
        if (_firstRender) return;

        var result = await Sender.Send(new GetProductQuery());

        result.Match(
            onSuccess: products => _products = products,
            onFailure: error => Snackbar.Add(error.Description, Severity.Error)
        );

        _loading = false;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;

        _firstRender = false;
        await OnInitializedAsync();
        StateHasChanged();
    }
}
