using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.Razor.Internal;
using MudBlazor;
using OrderSphere.Application.Features.Cart.AddToCart;
using OrderSphere.Application.Features.Product.GetProductBySlug;
using OrderSphere.Application.Models;
using OrderSphere.Domain.Entities;
using OrderSphere.Domain.Extensions;
using OrderSphere.UI.Configuration;
using OrderSphere.UI.Services;

namespace OrderSphere.UI.Components.Pages.Shopping;

public partial class ProductDetails : OrderSphereComponentBase
{
    [Parameter] public string Slug { get; set; } = default!;
    [Parameter] public string CategoryName { get; set; } = default!;

    private ProductDto? _product;
    private int _quantity = 1;
    private bool _wishlisted = false;


    protected override async Task LoadDataAsync()
    {
        var result = await Sender.Send(new GetProductBySlugQuery(Slug));
        var userInfo = await CurrentUserService.GetCurrentUserInfoAsync();
        result.Match(
            onSuccess: product => _product = product,
            onFailure: error => Snackbar.Add(error.Description, Severity.Error)
        );
    }

    private void IncreaseQuantity()
    {
        if (_product is null) return;
        if (_quantity < _product.Stock)
            _quantity++;
    }

    private void DecreaseQuantity()
    {
        if (_quantity > 1)
            _quantity--;
    }


    private async Task AddToCartAsync()
    {
        if(_product is null) return;

        var currentUser = await CurrentUserService.GetCurrentUserInfoAsync();
        var result = await Sender.Send(new AddToCartCommand((Guid)currentUser.CustomerId!, _product.Id, _quantity));

        if (!result.IsSuccess)
            return;

        await CartService.RefreshCartAsync();
    }

    private void ToggleWishlist()
    {
        _wishlisted = !_wishlisted;
        var msg = _wishlisted ? "Zur Wunschliste hinzugefügt" : "Von Wunschliste entfernt";
        Snackbar.Add(msg, Severity.Info);
    }
}
