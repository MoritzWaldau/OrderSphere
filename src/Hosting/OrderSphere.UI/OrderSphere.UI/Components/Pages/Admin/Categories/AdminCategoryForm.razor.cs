using Microsoft.AspNetCore.Components;
using MudBlazor;
using OrderSphere.Application.Features.Category.Admin.CreateCategory;
using OrderSphere.Application.Features.Category.Admin.GetAllCategoriesAdmin;
using OrderSphere.Application.Features.Category.Admin.UpdateCategory;
using OrderSphere.UI.Configuration;

namespace OrderSphere.UI.Components.Pages.Admin.Categories;

public partial class AdminCategoryForm : OrderSphereComponentBase
{
    [Parameter] public Guid? CategoryId { get; set; }
    [Inject] public required NavigationManager Navigation { get; set; }

    private bool IsEdit => CategoryId.HasValue;
    private MudForm? _form;
    private FormModel? _model;
    private bool _isSaving;

    protected override async Task LoadDataAsync()
    {
        if (IsEdit)
        {
            var result = await Sender.Send(new GetAllCategoriesAdminQuery());
            if (result.IsSuccess)
            {
                var existing = result.Value.FirstOrDefault(c => c.Id == CategoryId);
                if (existing is not null)
                {
                    _model = new FormModel
                    {
                        Name = existing.Name,
                        Description = existing.Description,
                        IsActive = existing.IsActive
                    };
                }
            }
        }
        else
        {
            _model = new FormModel { IsActive = true };
        }
    }

    private async Task SaveAsync()
    {
        if (_form is null || _model is null) return;
        await _form.ValidateAsync();
        if (!_form.IsValid) return;

        _isSaving = true;
        try
        {
            if (IsEdit)
            {
                var result = await Sender.Send(new UpdateCategoryCommand(CategoryId!.Value, _model.Name, _model.Description, _model.IsActive));
                if (result.IsSuccess)
                {
                    Snackbar.Add("Kategorie aktualisiert.", Severity.Success);
                    Navigation.NavigateTo("/admin/categories");
                }
                else Snackbar.Add(result.Error.Description, Severity.Error);
            }
            else
            {
                var result = await Sender.Send(new CreateCategoryCommand(_model.Name, _model.Description));
                if (result.IsSuccess)
                {
                    Snackbar.Add("Kategorie angelegt.", Severity.Success);
                    Navigation.NavigateTo("/admin/categories");
                }
                else Snackbar.Add(result.Error.Description, Severity.Error);
            }
        }
        finally
        {
            _isSaving = false;
        }
    }

    private sealed class FormModel
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}
