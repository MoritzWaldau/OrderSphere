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
    private bool _popupOpen = false;
    private ProductDto? _selectedProduct;

    protected override async Task LoadDataAsync()
    {
        var result = await Sender.Send(new GetProductQuery());

        result.Match(
            onSuccess: products => _products = products,
            onFailure: error => Snackbar.Add(error.Description, Severity.Error)
        );
    }

    private void OpenQuantityPopup(ProductDto product)
    {
        _selectedProduct = product;
        _popupOpen = true;
    }

    private void ClosePopup()
    {
        _popupOpen = false;
        _selectedProduct = null;
    }

    private void ConfirmAddToCart(int quantity)
    {
        if (_selectedProduct is null) return;

        Snackbar.Add($"{quantity}x {_selectedProduct.Name} wurde zum Warenkorb hinzugefügt.", Severity.Success);
        ClosePopup();
    }
}
