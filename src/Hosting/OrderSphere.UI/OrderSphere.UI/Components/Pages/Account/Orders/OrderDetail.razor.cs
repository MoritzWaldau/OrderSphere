using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using MudBlazor;
using OrderSphere.Application.Features.Order.GetOrderById;
using OrderSphere.Application.Models;
using OrderSphere.Domain.Enums;
using OrderSphere.UI.Configuration;

namespace OrderSphere.UI.Components.Pages.Account.Orders;

public partial class OrderDetail : OrderSphereComponentBase
{
    [Parameter] public Guid OrderId { get; set; }

    [Inject] public required IJSRuntime JS { get; set; }

    private OrderDto? _order;
    private bool _copied;

    private DateTime EstimatedShipDate => _order is null
        ? DateTime.UtcNow
        : _order.CreatedAt.AddDays(1);

    private DateTime EstimatedDeliveryStart => _order is null
        ? DateTime.UtcNow
        : _order.CreatedAt.AddDays(2);

    private DateTime EstimatedDeliveryEnd => _order is null
        ? DateTime.UtcNow
        : _order.CreatedAt.AddDays(4);

    protected override async Task LoadDataAsync()
    {
        var customerId = await CurrentUserService.GetCustomerIdAsync();
        if (customerId is null)
        {
            Snackbar.Add("Bitte melde dich an, um Bestelldetails zu sehen.", Severity.Warning);
            return;
        }

        var result = await Sender.Send(new GetOrderByIdQuery(OrderId, customerId.Value));

        if (result.IsSuccess)
        {
            _order = result.Value;
        }
        else
        {
            // Order not found or access denied — keep _order null so the empty state renders
            _order = null;
        }
    }

    private async Task CopyTrackingNumberAsync()
    {
        if (_order?.TrackingNumber is null) return;

        try
        {
            await JS.InvokeVoidAsync("navigator.clipboard.writeText", _order.TrackingNumber);
            _copied = true;
            Snackbar.Add("Tracking-Nummer kopiert.", Severity.Success);

            // Reset icon after 2 seconds
            await Task.Delay(2000);
            _copied = false;
            StateHasChanged();
        }
        catch
        {
            Snackbar.Add("Kopieren in die Zwischenablage fehlgeschlagen.", Severity.Error);
        }
    }

    private static string GetPaymentLabel(PaymentMethod method) => method switch
    {
        PaymentMethod.CreditCard => "Kreditkarte",
        PaymentMethod.PayPal => "PayPal",
        PaymentMethod.Invoice => "Rechnung",
        _ => method.ToString()
    };

    private (string Label, string Description, string Icon, bool IsActive, bool IsCompleted)[] StatusSteps
    {
        get
        {
            if (_order is null) return Array.Empty<(string, string, string, bool, bool)>();

            int currentIndex = _order.Status switch
            {
                OrderStatus.Created => 0,
                OrderStatus.Paid => 1,
                OrderStatus.Shipped => 2,
                OrderStatus.Delivered => 3,
                OrderStatus.Cancelled => -1,
                _ => 0
            };

            if (_order.Status == OrderStatus.Cancelled)
            {
                return new[]
                {
                    ("Storniert", "Diese Bestellung wurde storniert.", Icons.Material.Outlined.Cancel, true, true)
                };
            }

            return new[]
            {
                ("Eingegangen", "Wir haben deine Bestellung erhalten.", Icons.Material.Outlined.Receipt, currentIndex == 0, currentIndex > 0),
                ("Bezahlt", "Zahlung erfolgreich verbucht.", Icons.Material.Outlined.Payments, currentIndex == 1, currentIndex > 1),
                ("Versendet", "Deine Bestellung ist auf dem Weg.", Icons.Material.Outlined.LocalShipping, currentIndex == 2, currentIndex > 2),
                ("Zugestellt", "Sendung wurde zugestellt.", Icons.Material.Outlined.HomeWork, currentIndex == 3, currentIndex > 3)
            };
        }
    }
}
