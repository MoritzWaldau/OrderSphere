using MudBlazor;
using OrderSphere.Application.Features.Order.GetOrdersByCustomer;
using OrderSphere.Application.Models;
using OrderSphere.Domain.Enums;
using OrderSphere.UI.Configuration;

namespace OrderSphere.UI.Components.Pages.Account.Orders;

public partial class Orders : OrderSphereComponentBase
{
    private IReadOnlyList<OrderDto> _orders = Array.Empty<OrderDto>();

    protected override async Task LoadDataAsync()
    {
        var customerId = await CurrentUserService.GetCustomerIdAsync();

        if (customerId is null)
        {
            Snackbar.Add("Bitte melde dich an, um deine Bestellungen zu sehen.", Severity.Warning);
            return;
        }

        var result = await Sender.Send(new GetOrdersByCustomerQuery(customerId.Value));

        if (result.IsSuccess)
        {
            _orders = result.Value;
        }
        else
        {
            Snackbar.Add(result.Error.Description, Severity.Error);
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
