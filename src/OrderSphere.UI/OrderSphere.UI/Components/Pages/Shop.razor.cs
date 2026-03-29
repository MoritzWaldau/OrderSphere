using MediatR;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using OrderSphere.Application.Features.Product.GetProduct;
using OrderSphere.Application.Features.Cart.AddToCart;
using OrderSphere.Application.Features.Cart.GetCart;
using OrderSphere.Application.Models;
using OrderSphere.Domain.Extensions;
using OrderSphere.UI.Configuration;
using OrderSphere.UI.Services;

namespace OrderSphere.UI.Components.Pages;

public partial class Shop : OrderSphereComponentBase
{
    [Inject] private ICartService CartService { get; set; } = null!;

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

    private async Task ConfirmAddToCartAsync(int quantity)
    {
        if (_selectedProduct is null) return;

        var customerId = GetCustomerId();
        var command = new AddToCartCommand(customerId, _selectedProduct.Id, quantity);
        var result = await Sender.Send(command);

        if (result.IsSuccess)
        {
            Snackbar.Add($"{quantity}x {_selectedProduct.Name} wurde zum Warenkorb hinzugefügt.", Severity.Success);

            // Refresh cart in the drawer
            var cartResult = await Sender.Send(new GetCartQuery(customerId));
            if (cartResult.IsSuccess)
            {
                CartService.UpdateCart(cartResult.Value);
            }

            ClosePopup();
        }
        else
        {
            Snackbar.Add(result.Error.Description, Severity.Error);
        }
    }

    private Guid GetCustomerId()
    {
        // TODO: Replace with actual user/customer ID from authentication
        return Guid.Parse("550e8400-e29b-41d4-a716-446655440000");
    }
}
