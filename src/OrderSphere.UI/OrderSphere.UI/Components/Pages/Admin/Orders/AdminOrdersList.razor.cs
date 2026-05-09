using MudBlazor;
using OrderSphere.Application.Features.Order.Admin.GetAllOrders;
using OrderSphere.Application.Models;
using OrderSphere.Domain.Enums;
using OrderSphere.UI.Configuration;

namespace OrderSphere.UI.Components.Pages.Admin.Orders;

public partial class AdminOrdersList : OrderSphereComponentBase
{
    private IReadOnlyList<OrderDto> _orders = Array.Empty<OrderDto>();
    private OrderStatus? _statusFilter;

    protected override async Task LoadDataAsync()
    {
        var result = await Sender.Send(new GetAllOrdersQuery(_statusFilter));

        if (result.IsSuccess)
        {
            _orders = result.Value;
        }
        else
        {
            Snackbar.Add(result.Error.Description, Severity.Error);
        }
    }

    private async Task OnStatusFilterChanged(OrderStatus? newValue)
    {
        _statusFilter = newValue;
        await LoadDataAsync();
        StateHasChanged();
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
