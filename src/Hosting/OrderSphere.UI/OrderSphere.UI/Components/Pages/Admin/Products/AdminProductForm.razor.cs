using Microsoft.AspNetCore.Components;
using MudBlazor;
using OrderSphere.Application.Features.Category.GetCategories;
using OrderSphere.Application.Features.Product.Admin.CreateProduct;
using OrderSphere.Application.Features.Product.Admin.GetProductByIdAdmin;
using OrderSphere.Application.Features.Product.Admin.UpdateProduct;
using OrderSphere.Application.Models;
using OrderSphere.Application.Models.Admin;
using OrderSphere.UI.Configuration;

namespace OrderSphere.UI.Components.Pages.Admin.Products;

public partial class AdminProductForm : OrderSphereComponentBase
{
    [Parameter] public Guid? ProductId { get; set; }
    [Inject] public required NavigationManager Navigation { get; set; }

    private bool IsEdit => ProductId.HasValue;
    private MudForm? _form;
    private ProductFormModel? _model;
    private List<CategoryDto> _categories = [];
    private bool _isSaving;

    protected override async Task LoadDataAsync()
    {
        var categoriesResult = await Sender.Send(new GetCategoriesQuery());
        if (categoriesResult.IsSuccess)
        {
            _categories = categoriesResult.Value.ToList();
        }

        if (IsEdit)
        {
            var productResult = await Sender.Send(new GetProductByIdAdminQuery(ProductId!.Value));
            if (productResult.IsSuccess)
            {
                var p = productResult.Value;
                _model = new ProductFormModel
                {
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    Stock = p.Stock,
                    CategoryId = p.CategoryId,
                    SKU = p.SKU,
                    IsActive = p.IsActive
                };
            }
            else
            {
                _model = null;
            }
        }
        else
        {
            _model = new ProductFormModel
            {
                CategoryId = _categories.FirstOrDefault()?.Id ?? Guid.Empty,
                IsActive = true
            };
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
            var input = new AdminProductInput(
                _model.Name,
                _model.Description,
                _model.Price,
                _model.Stock,
                _model.CategoryId,
                _model.SKU);

            if (IsEdit)
            {
                var result = await Sender.Send(new UpdateProductCommand(ProductId!.Value, input, _model.IsActive));
                if (result.IsSuccess)
                {
                    Snackbar.Add("Produkt aktualisiert.", Severity.Success);
                    Navigation.NavigateTo("/admin/products");
                }
                else
                {
                    Snackbar.Add(result.Error.Description, Severity.Error);
                }
            }
            else
            {
                var result = await Sender.Send(new CreateProductCommand(input));
                if (result.IsSuccess)
                {
                    Snackbar.Add("Produkt angelegt.", Severity.Success);
                    Navigation.NavigateTo("/admin/products");
                }
                else
                {
                    Snackbar.Add(result.Error.Description, Severity.Error);
                }
            }
        }
        finally
        {
            _isSaving = false;
        }
    }

    private sealed class ProductFormModel
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; } = 0.01m;
        public int Stock { get; set; }
        public Guid CategoryId { get; set; }
        public string SKU { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
    }
}
