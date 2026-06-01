using Azure.Messaging.ServiceBus;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OrderSphere.BuildingBlocks.Contracts.Events;
using OrderSphere.BuildingBlocks.EventBus.Inbox;
using OrderSphere.Notification.Worker.Email;
using OrderSphere.Notification.Worker.Workers;
using Xunit;

namespace OrderSphere.Notification.Tests;

public sealed class NotificationProcessorTests
{
    private static NotificationProcessor NewProcessor()
        => new(
            Substitute.For<ServiceBusClient>(),
            Substitute.For<IServiceScopeFactory>(),
            NullLogger<NotificationProcessor>.Instance);

    private static OrderPlacedIntegrationEvent NewEvent(string email = "customer@example.com")
        => new()
        {
            OrderId = Guid.NewGuid(),
            CustomerEmail = email,
            CustomerName = "Max Mustermann",
            TrackingNumber = "TRK-001",
            ShippingFirstName = "Max",
            ShippingLastName = "Mustermann",
            ShippingStreet = "Hauptstr. 1",
            ShippingCity = "Berlin",
            ShippingPostalCode = "10115",
            ShippingCountry = "Deutschland",
            Items = [new OrderPlacedItemDto("Widget", 1, 9.99m)],
            Total = 9.99m
        };

    [Fact]
    public async Task Happy_path_sends_email_and_marks_inbox_processed()
    {
        var evt = NewEvent();
        var inbox = Substitute.For<IInboxStore>();
        inbox.HasBeenProcessedAsync(evt.Id, Arg.Any<CancellationToken>()).Returns(false);
        var emailService = Substitute.For<INotificationEmailService>();

        await NewProcessor().ProcessNotificationAsync(evt, inbox, emailService, CancellationToken.None);

        await emailService.Received(1).SendOrderConfirmationAsync(evt, Arg.Any<CancellationToken>());
        await inbox.Received(1).MarkAsProcessedAsync(
            evt.Id, nameof(OrderPlacedIntegrationEvent), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Duplicate_event_skips_email_and_does_not_mark_inbox()
    {
        var evt = NewEvent();
        var inbox = Substitute.For<IInboxStore>();
        inbox.HasBeenProcessedAsync(evt.Id, Arg.Any<CancellationToken>()).Returns(true);
        var emailService = Substitute.For<INotificationEmailService>();

        await NewProcessor().ProcessNotificationAsync(evt, inbox, emailService, CancellationToken.None);

        await emailService.DidNotReceive().SendOrderConfirmationAsync(Arg.Any<OrderPlacedIntegrationEvent>(), Arg.Any<CancellationToken>());
        await inbox.DidNotReceive().MarkAsProcessedAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Missing_email_marks_inbox_without_sending()
    {
        var evt = NewEvent(email: "");
        var inbox = Substitute.For<IInboxStore>();
        inbox.HasBeenProcessedAsync(evt.Id, Arg.Any<CancellationToken>()).Returns(false);
        var emailService = Substitute.For<INotificationEmailService>();

        await NewProcessor().ProcessNotificationAsync(evt, inbox, emailService, CancellationToken.None);

        await emailService.DidNotReceive().SendOrderConfirmationAsync(Arg.Any<OrderPlacedIntegrationEvent>(), Arg.Any<CancellationToken>());
        await inbox.Received(1).MarkAsProcessedAsync(
            evt.Id, nameof(OrderPlacedIntegrationEvent), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Email_service_throws_propagates_and_does_not_mark_inbox()
    {
        var evt = NewEvent();
        var inbox = Substitute.For<IInboxStore>();
        inbox.HasBeenProcessedAsync(evt.Id, Arg.Any<CancellationToken>()).Returns(false);
        var emailService = Substitute.For<INotificationEmailService>();
        emailService.SendOrderConfirmationAsync(Arg.Any<OrderPlacedIntegrationEvent>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("SMTP unreachable"));

        Func<Task> act = () => NewProcessor().ProcessNotificationAsync(evt, inbox, emailService, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("SMTP unreachable");
        await inbox.DidNotReceive().MarkAsProcessedAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
