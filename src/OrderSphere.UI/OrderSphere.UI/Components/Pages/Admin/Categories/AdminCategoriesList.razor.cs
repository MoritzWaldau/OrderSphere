using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using OrderSphere.Application.Features.Category.Admin.DeleteCategory;
using OrderSphere.Application.Features.Category.Admin.GetAllCategoriesAdmin;
using OrderSphere.Application.Models.Admin;
using OrderSphere.UI.Configuration;

namespace OrderSphere.UI.Components.Pages.Admin.Categories;

public partial class AdminCategoriesList : OrderSphereComponentBase
{
    [Inject] public required IJSRuntime JS { get; set; }

    private IReadOnlyList<AdminCategoryDto> _categories = Array.Empty<AdminCategoryDto>();

    protected override async Task LoadDataAsync()
    {
        var result = await Sender.Send(new GetAllCategoriesAdminQuery());
        if (result.IsSuccess) _categories = result.Value;
        else Snackbar.Add(result.Error.Description, Severity.Error);
    }

    private async Task DeleteAsync(Guid id, string name)
    {
        var confirmed = await JS.InvokeAsync<bool>("confirm", $"Kategorie '{name}' wirklich löschen?");
        if (!confirmed) return;

        var result = await Sender.Send(new DeleteCategoryCommand(id));
        if (result.IsSuccess)
        {
            Snackbar.Add("Kategorie gelöscht.", Severity.Success);
            await LoadDataAsync();
            StateHasChanged();
        }
        else
        {
            Snackbar.Add(result.Error.Description, Severity.Error);
        }
    }
}
