using Azure.Messaging.ServiceBus;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using OrderSphere.BuildingBlocks.Contracts.Events;
using OrderSphere.BuildingBlocks.EventBus.Inbox;
using OrderSphere.Notification.Worker.Channels;
using OrderSphere.Notification.Worker.Clients;
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

    private static INotificationChannel EmailChannel(INotificationChannel? substitute = null)
    {
        var ch = substitute ?? Substitute.For<INotificationChannel>();
        ch.ChannelType.Returns(NotificationChannelType.Email);
        return ch;
    }

    private static INotificationChannel SmsChannel(INotificationChannel? substitute = null)
    {
        var ch = substitute ?? Substitute.For<INotificationChannel>();
        ch.ChannelType.Returns(NotificationChannelType.Sms);
        return ch;
    }

    private static INotificationChannel PushChannel(INotificationChannel? substitute = null)
    {
        var ch = substitute ?? Substitute.For<INotificationChannel>();
        ch.ChannelType.Returns(NotificationChannelType.Push);
        return ch;
    }

    private static IUserProfileClient ProfileClient(
        bool emailEnabled = true, bool smsEnabled = false, bool pushEnabled = false)
    {
        var client = Substitute.For<IUserProfileClient>();
        client.GetNotificationPreferencesAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new NotificationPreferences(emailEnabled, smsEnabled, pushEnabled));
        return client;
    }


    [Fact]
    public async Task Happy_path_sends_email_and_marks_inbox_processed()
    {
        var evt = NewEvent();
        var inbox = Substitute.For<IInboxStore>();
        inbox.HasBeenProcessedAsync(evt.Id, Arg.Any<CancellationToken>()).Returns(false);
        var emailCh = EmailChannel();
        var channels = new[] { emailCh, SmsChannel(), PushChannel() };

        await NewProcessor().ProcessNotificationAsync(
            evt, inbox, channels, ProfileClient(emailEnabled: true), CancellationToken.None);

        await emailCh.Received(1).SendOrderConfirmationAsync(evt, Arg.Any<CancellationToken>());
        await inbox.Received(1).MarkAsProcessedAsync(
            evt.Id, nameof(OrderPlacedIntegrationEvent), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Duplicate_event_skips_channels_and_does_not_mark_inbox()
    {
        var evt = NewEvent();
        var inbox = Substitute.For<IInboxStore>();
        inbox.HasBeenProcessedAsync(evt.Id, Arg.Any<CancellationToken>()).Returns(true);
        var emailCh = EmailChannel();

        await NewProcessor().ProcessNotificationAsync(
            evt, inbox, [emailCh], ProfileClient(), CancellationToken.None);

        await emailCh.DidNotReceive().SendOrderConfirmationAsync(Arg.Any<OrderPlacedIntegrationEvent>(), Arg.Any<CancellationToken>());
        await inbox.DidNotReceive().MarkAsProcessedAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Missing_email_marks_inbox_without_sending()
    {
        var evt = NewEvent(email: "");
        var inbox = Substitute.For<IInboxStore>();
        inbox.HasBeenProcessedAsync(evt.Id, Arg.Any<CancellationToken>()).Returns(false);
        var emailCh = EmailChannel();

        await NewProcessor().ProcessNotificationAsync(
            evt, inbox, [emailCh], ProfileClient(), CancellationToken.None);

        await emailCh.DidNotReceive().SendOrderConfirmationAsync(Arg.Any<OrderPlacedIntegrationEvent>(), Arg.Any<CancellationToken>());
        await inbox.Received(1).MarkAsProcessedAsync(
            evt.Id, nameof(OrderPlacedIntegrationEvent), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Channel_throws_propagates_and_does_not_mark_inbox()
    {
        var evt = NewEvent();
        var inbox = Substitute.For<IInboxStore>();
        inbox.HasBeenProcessedAsync(evt.Id, Arg.Any<CancellationToken>()).Returns(false);
        var emailCh = EmailChannel();
        emailCh.SendOrderConfirmationAsync(Arg.Any<OrderPlacedIntegrationEvent>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new InvalidOperationException("SMTP unreachable"));

        Func<Task> act = () => NewProcessor().ProcessNotificationAsync(
            evt, inbox, [emailCh], ProfileClient(), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("SMTP unreachable");
        await inbox.DidNotReceive().MarkAsProcessedAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Sms_and_push_channels_invoked_when_preferences_enabled()
    {
        var evt = NewEvent();
        var inbox = Substitute.For<IInboxStore>();
        inbox.HasBeenProcessedAsync(evt.Id, Arg.Any<CancellationToken>()).Returns(false);

        var emailCh = EmailChannel();
        var smsCh = SmsChannel();
        var pushCh = PushChannel();

        await NewProcessor().ProcessNotificationAsync(
            evt, inbox, [emailCh, smsCh, pushCh],
            ProfileClient(emailEnabled: true, smsEnabled: true, pushEnabled: true),
            CancellationToken.None);

        await emailCh.Received(1).SendOrderConfirmationAsync(evt, Arg.Any<CancellationToken>());
        await smsCh.Received(1).SendOrderConfirmationAsync(evt, Arg.Any<CancellationToken>());
        await pushCh.Received(1).SendOrderConfirmationAsync(evt, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Only_opted_in_channels_are_invoked()
    {
        var evt = NewEvent();
        var inbox = Substitute.For<IInboxStore>();
        inbox.HasBeenProcessedAsync(evt.Id, Arg.Any<CancellationToken>()).Returns(false);

        var emailCh = EmailChannel();
        var smsCh = SmsChannel();
        var pushCh = PushChannel();

        // Only email is enabled; SMS and Push are opted out.
        await NewProcessor().ProcessNotificationAsync(
            evt, inbox, [emailCh, smsCh, pushCh],
            ProfileClient(emailEnabled: true, smsEnabled: false, pushEnabled: false),
            CancellationToken.None);

        await emailCh.Received(1).SendOrderConfirmationAsync(evt, Arg.Any<CancellationToken>());
        await smsCh.DidNotReceive().SendOrderConfirmationAsync(Arg.Any<OrderPlacedIntegrationEvent>(), Arg.Any<CancellationToken>());
        await pushCh.DidNotReceive().SendOrderConfirmationAsync(Arg.Any<OrderPlacedIntegrationEvent>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UserProfile_unavailable_falls_back_to_email_only()
    {
        var evt = NewEvent();
        var inbox = Substitute.For<IInboxStore>();
        inbox.HasBeenProcessedAsync(evt.Id, Arg.Any<CancellationToken>()).Returns(false);

        var emailCh = EmailChannel();
        var smsCh = SmsChannel();

        // FallbackUserProfileClient returns email-only defaults.
        var fallback = new FallbackUserProfileClient(
            NullLogger<FallbackUserProfileClient>.Instance);

        await NewProcessor().ProcessNotificationAsync(
            evt, inbox, [emailCh, smsCh], fallback, CancellationToken.None);

        await emailCh.Received(1).SendOrderConfirmationAsync(evt, Arg.Any<CancellationToken>());
        await smsCh.DidNotReceive().SendOrderConfirmationAsync(Arg.Any<OrderPlacedIntegrationEvent>(), Arg.Any<CancellationToken>());
    }
}
