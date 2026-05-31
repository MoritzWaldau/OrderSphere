using Azure.Messaging.ServiceBus;
using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using OrderSphere.BuildingBlocks.Contracts.Events;
using OrderSphere.BuildingBlocks.Primitives;
using OrderSphere.BuildingBlocks.StronglyTypedIds;
using OrderSphere.Payment.Domain.Entities;
using OrderSphere.Payment.Domain.Enums;
using OrderSphere.Payment.Infrastructure.Persistence;
using OrderSphere.Payment.Infrastructure.Providers;
using OrderSphere.Payment.Worker.Workers;
using Xunit;

namespace OrderSphere.Payment.Tests;

public sealed class PaymentProcessorTests
{
    private const string Method = "creditcard";

    private static PaymentDbContext NewContext()
    {
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new PaymentDbContext(options, Substitute.For<IPublisher>());
    }

    private static PaymentProcessor NewProcessor()
        => new(
            Substitute.For<ServiceBusClient>(),
            Substitute.For<IServiceScopeFactory>(),
            NullLogger<PaymentProcessor>.Instance);

    private static PaymentRequestedIntegrationEvent NewEvent(string method = Method)
        => new()
        {
            OrderId = Guid.NewGuid(),
            Amount = 49.99m,
            Currency = "EUR",
            PaymentMethod = method,
            CustomerEmail = "customer@example.com"
        };

    private static IPaymentProviderFactory FactoryReturning(IPaymentProvider? provider, string method = Method)
    {
        var factory = Substitute.For<IPaymentProviderFactory>();
        factory.GetProvider(method).Returns(provider);
        return factory;
    }

    [Fact]
    public async Task Authorize_and_capture_succeed_persists_captured_record_and_returns_true()
    {
        await using var context = NewContext();
        var evt = NewEvent();

        var provider = Substitute.For<IPaymentProvider>();
        provider.AuthorizeAsync(Arg.Any<PaymentRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<PaymentProviderResult>.Success(new PaymentProviderResult("auth-1")));
        provider.CaptureAsync(Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<CancellationToken>())
            .Returns(Result<PaymentProviderResult>.Success(new PaymentProviderResult("cap-1")));

        var result = await NewProcessor().ProcessPaymentAsync(
            evt, context, FactoryReturning(provider), CancellationToken.None);

        result.Should().BeTrue();

        var record = await context.Payments.SingleAsync(p => p.OrderId == OrderId.From(evt.OrderId));
        record.Status.Should().Be(PaymentStatus.Captured);
        record.TransactionId.Should().Be("cap-1");
    }

    [Fact]
    public async Task Unsupported_payment_method_persists_failed_record_and_returns_false()
    {
        await using var context = NewContext();
        var evt = NewEvent("bitcoin");

        var result = await NewProcessor().ProcessPaymentAsync(
            evt, context, FactoryReturning(provider: null, method: "bitcoin"), CancellationToken.None);

        result.Should().BeFalse();

        var record = await context.Payments.SingleAsync(p => p.OrderId == OrderId.From(evt.OrderId));
        record.Status.Should().Be(PaymentStatus.Failed);
        record.FailureReason.Should().Contain("bitcoin");
    }

    [Fact]
    public async Task Authorization_failure_persists_failed_record_and_returns_false()
    {
        await using var context = NewContext();
        var evt = NewEvent();

        var provider = Substitute.For<IPaymentProvider>();
        provider.AuthorizeAsync(Arg.Any<PaymentRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<PaymentProviderResult>.Failure(new Error("Payment.Auth", "Card declined.")));

        var result = await NewProcessor().ProcessPaymentAsync(
            evt, context, FactoryReturning(provider), CancellationToken.None);

        result.Should().BeFalse();

        var record = await context.Payments.SingleAsync(p => p.OrderId == OrderId.From(evt.OrderId));
        record.Status.Should().Be(PaymentStatus.Failed);
        record.FailureReason.Should().Be("Card declined.");
        await provider.DidNotReceive().CaptureAsync(Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Capture_failure_persists_failed_record_and_returns_false()
    {
        await using var context = NewContext();
        var evt = NewEvent();

        var provider = Substitute.For<IPaymentProvider>();
        provider.AuthorizeAsync(Arg.Any<PaymentRequest>(), Arg.Any<CancellationToken>())
            .Returns(Result<PaymentProviderResult>.Success(new PaymentProviderResult("auth-1")));
        provider.CaptureAsync(Arg.Any<string>(), Arg.Any<decimal>(), Arg.Any<CancellationToken>())
            .Returns(Result<PaymentProviderResult>.Failure(new Error("Payment.Capture", "Capture rejected.")));

        var result = await NewProcessor().ProcessPaymentAsync(
            evt, context, FactoryReturning(provider), CancellationToken.None);

        result.Should().BeFalse();

        var record = await context.Payments.SingleAsync(p => p.OrderId == OrderId.From(evt.OrderId));
        record.Status.Should().Be(PaymentStatus.Failed);
        record.FailureReason.Should().Be("Capture rejected.");
    }

    [Fact]
    public async Task Existing_captured_payment_is_idempotent_and_does_not_insert_duplicate()
    {
        await using var context = NewContext();
        var evt = NewEvent();

        var existing = new PaymentRecord(
            OrderId.From(evt.OrderId), evt.Amount, evt.Currency, evt.PaymentMethod, evt.CustomerEmail, evt.CorrelationId);
        existing.MarkCaptured("pre-existing");
        context.Payments.Add(existing);
        await context.SaveChangesAsync();

        var provider = Substitute.For<IPaymentProvider>();

        var result = await NewProcessor().ProcessPaymentAsync(
            evt, context, FactoryReturning(provider), CancellationToken.None);

        result.Should().BeTrue();
        (await context.Payments.CountAsync(p => p.OrderId == OrderId.From(evt.OrderId))).Should().Be(1);
        await provider.DidNotReceive().AuthorizeAsync(Arg.Any<PaymentRequest>(), Arg.Any<CancellationToken>());
    }
}
