using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using OrderSphere.Application.Features.Product.Admin.DeleteProduct;
using OrderSphere.Application.Features.Product.Admin.GetAllProductsAdmin;
using OrderSphere.Application.Models.Admin;
using OrderSphere.UI.Configuration;

namespace OrderSphere.UI.Components.Pages.Admin.Products;

public partial class AdminProductsList : OrderSphereComponentBase
{
    [Inject] public required IJSRuntime JS { get; set; }

    private IReadOnlyList<AdminProductDto> _products = Array.Empty<AdminProductDto>();

    protected override async Task LoadDataAsync()
    {
        var result = await Sender.Send(new GetAllProductsAdminQuery());
        if (result.IsSuccess)
        {
            _products = result.Value;
        }
        else
        {
            Snackbar.Add(result.Error.Description, Severity.Error);
        }
    }

    private async Task DeleteProductAsync(Guid productId, string productName)
    {
        var confirmed = await JS.InvokeAsync<bool>("confirm",
            $"Produkt '{productName}' wirklich löschen?");
        if (!confirmed) return;

        var result = await Sender.Send(new DeleteProductCommand(productId));
        if (result.IsSuccess)
        {
            Snackbar.Add("Produkt gelöscht.", Severity.Success);
            await LoadDataAsync();
            StateHasChanged();
        }
        else
        {
            Snackbar.Add(result.Error.Description, Severity.Error);
        }
    }
}
