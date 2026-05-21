using MediatR;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using OrderSphere.Application.Features.Product.GetProduct;
using OrderSphere.Application.Features.Cart.AddToCart;
using OrderSphere.Application.Features.Cart.GetCart;
using OrderSphere.Application.Models;
using OrderSphere.UI.Configuration;
using OrderSphere.UI.Services;
using OrderSphere.UI.Services.Auth;
using OrderSphere.BuildingBlocks.Extensions;

namespace OrderSphere.UI.Components.Pages.Shopping;

public partial class Shop : OrderSphereComponentBase
{
    [Parameter]
    public string Category { get; set; } = string.Empty;

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

    private void OpenDetails(ProductDto product)
    {
        _selectedProduct = product;
         NavManager.NavigateTo($"/shop/{_selectedProduct?.CategoryName}/{_selectedProduct?.Slug}");
    }
     
    private async Task ConfirmAddToCartAsync(int quantity)
    {
        if (_selectedProduct is null) return;

        var customerId = await CurrentUserService.GetCustomerIdAsync();
        if (customerId is null) return;

        var command = new AddToCartCommand(customerId.Value, _selectedProduct.Id, quantity);
        var result = await Sender.Send(command);

        if (result.IsSuccess)
        {
            Snackbar.Add($"{quantity}x {_selectedProduct.Name} wurde zum Warenkorb hinzugefügt.", Severity.Success);

            var cartResult = await Sender.Send(new GetCartQuery(customerId.Value));
            if (cartResult.IsSuccess)
            {
                CartService.UpdateCart(customerId.Value, cartResult.Value);
            }

            ClosePopup();
        }
        else
        {
            Snackbar.Add(result.Error.Description, Severity.Error);
        }
    }
}
