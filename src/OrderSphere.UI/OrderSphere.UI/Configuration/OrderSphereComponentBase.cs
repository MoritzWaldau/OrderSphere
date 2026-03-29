using MediatR;
using Microsoft.AspNetCore.Components;

namespace OrderSphere.UI.Configuration;

public abstract class OrderSphereComponentBase : ComponentBase
{
    [Inject] public required ISender Sender { get; set; }

    private bool _firstRender = true;
    protected bool IsLoading { get; private set; } = true;

    protected override async Task OnInitializedAsync()
    {
        if (_firstRender) return;
        await LoadDataAsync();
        IsLoading = false;
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        _firstRender = false;
        await OnInitializedAsync();
        StateHasChanged();
    }

    protected virtual Task LoadDataAsync() => Task.CompletedTask;
}
