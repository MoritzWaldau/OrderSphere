using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using OrderSphere.Application.Features.Order.Admin.CancelOrder;
using OrderSphere.Application.Features.Order.Admin.GetOrderByIdAdmin;
using OrderSphere.Application.Features.Order.Admin.UpdateOrderStatus;
using OrderSphere.Application.Models;
using OrderSphere.Domain.Enums;
using OrderSphere.UI.Configuration;

namespace OrderSphere.UI.Components.Pages.Admin.Orders;

public partial class AdminOrderDetail : OrderSphereComponentBase
{
    [Parameter] public Guid OrderId { get; set; }

    [Inject] public required IJSRuntime JS { get; set; }

    private OrderDto? _order;
    private bool _isProcessing;

    protected override async Task LoadDataAsync()
    {
        var result = await Sender.Send(new GetOrderByIdAdminQuery(OrderId));
        if (result.IsSuccess)
        {
            _order = result.Value;
        }
        else
        {
            _order = null;
        }
    }

    private async Task MarkShipped() =>
        await UpdateStatus(OrderStatus.Shipped, "Bestellung als versendet markiert.");

    private async Task MarkDelivered() =>
        await UpdateStatus(OrderStatus.Delivered, "Bestellung als zugestellt markiert.");

    private async Task UpdateStatus(OrderStatus newStatus, string successMessage)
    {
        if (_order is null) return;

        _isProcessing = true;
        try
        {
            var result = await Sender.Send(new UpdateOrderStatusCommand(OrderId, newStatus));
            if (result.IsSuccess)
            {
                Snackbar.Add(successMessage, Severity.Success);
                await LoadDataAsync();
            }
            else
            {
                Snackbar.Add(result.Error.Description, Severity.Error);
            }
        }
        finally
        {
            _isProcessing = false;
            StateHasChanged();
        }
    }

    private async Task OpenCancelDialog()
    {
        if (_order is null) return;

        var confirmed = await JS.InvokeAsync<bool>("confirm",
            "Bestellung wirklich stornieren? Stock wird zurückgegeben.");

        if (!confirmed) return;

        _isProcessing = true;
        try
        {
            var result = await Sender.Send(new CancelOrderCommand(OrderId));
            if (result.IsSuccess)
            {
                Snackbar.Add("Bestellung storniert.", Severity.Success);
                await LoadDataAsync();
            }
            else
            {
                Snackbar.Add(result.Error.Description, Severity.Error);
            }
        }
        finally
        {
            _isProcessing = false;
            StateHasChanged();
        }
    }

    private static string GetStatusLabel(OrderStatus status) => status switch
    {
        OrderStatus.Created => "Eingegangen",
        OrderStatus.Paid => "Bezahlt",
        OrderStatus.Shipped => "Versendet",
        OrderStatus.Delivered => "Zugestellt",
        OrderStatus.Cancelled => "Storniert",
        _ => status.ToString()
    };

    private static Color GetStatusColor(OrderStatus status) => status switch
    {
        OrderStatus.Created => Color.Info,
        OrderStatus.Paid => Color.Primary,
        OrderStatus.Shipped => Color.Warning,
        OrderStatus.Delivered => Color.Success,
        OrderStatus.Cancelled => Color.Error,
        _ => Color.Default
    };
}
