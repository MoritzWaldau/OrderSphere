using System.Globalization;
using FluentAssertions;
using OrderSphere.BuildingBlocks.Contracts.Events;
using OrderSphere.Notification.Worker.Email;
using Xunit;

namespace OrderSphere.Notification.Tests;

public sealed class NotificationEmailContentTests
{
    private static OrderPlacedIntegrationEvent NewEvent() => new()
    {
        OrderId = Guid.NewGuid(),
        CustomerEmail = "customer@example.com",
        CustomerName = "Erika Mustermann",
        TrackingNumber = "TRK-12345",
        ShippingFirstName = "Erika",
        ShippingLastName = "Mustermann",
        ShippingStreet = "Hauptstraße 1",
        ShippingCity = "Berlin",
        ShippingPostalCode = "10115",
        ShippingCountry = "Deutschland",
        Items =
        [
            new OrderPlacedItemDto("Mechanical Keyboard", 2, 79.50m),
            new OrderPlacedItemDto("USB-C Cable", 1, 9.99m)
        ],
        Total = 168.99m
    };

    [Fact]
    public void Plain_text_contains_order_tracking_address_items_and_total()
    {
        var evt = NewEvent();
        var de = CultureInfo.GetCultureInfo("de-DE");

        var text = NotificationEmailService.BuildPlainText(evt);

        text.Should().Contain(evt.OrderId.ToString());
        text.Should().Contain("TRK-12345");
        text.Should().Contain("Erika Mustermann");
        text.Should().Contain("10115 Berlin");
        text.Should().Contain("2x Mechanical Keyboard");
        text.Should().Contain("1x USB-C Cable");
        text.Should().Contain(evt.Total.ToString("C", de));
    }

    [Fact]
    public void Html_contains_each_item_row_and_formatted_total()
    {
        var evt = NewEvent();
        var de = CultureInfo.GetCultureInfo("de-DE");

        var html = NotificationEmailService.BuildHtml(evt);

        html.Should().Contain("Mechanical Keyboard");
        html.Should().Contain("USB-C Cable");
        html.Should().Contain(evt.TrackingNumber);
        html.Should().Contain(evt.Total.ToString("C", de));
        html.Should().Contain("<html>");
    }
}
