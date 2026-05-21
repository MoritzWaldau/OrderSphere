using MediatR;
using Microsoft.AspNetCore.Components;

namespace OrderSphere.UI.Configuration;

public abstract class OrderSphereComponentBase : ComponentBase
{
    [Inject] public required ISender Sender { get; set; }

    protected bool IsLoading { get; private set; } = true;

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender) return;
        await LoadDataAsync();
        IsLoading = false;
        StateHasChanged();
    }

    protected virtual Task LoadDataAsync() => Task.CompletedTask;
}
