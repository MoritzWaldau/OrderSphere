using System.Collections.Concurrent;
using OrderSphere.Application.Abstraction;
using OrderSphere.Application.Models;

namespace OrderSphere.IntegrationTests;

public sealed class CapturingEmailService : IEmailService
{
    public ConcurrentBag<(string ToEmail, string ResetLink)> Links { get; } = new();
    public ConcurrentBag<(string ToEmail, OrderConfirmationData Data)> OrderConfirmations { get; } = new();

    public Task SendLinkAsync(string toEmail, string resetLink)
    {
        Links.Add((toEmail, resetLink));
        return Task.CompletedTask;
    }

    public Task SendOrderConfirmationAsync(string toEmail, OrderConfirmationData data)
    {
        OrderConfirmations.Add((toEmail, data));
        return Task.CompletedTask;
    }
}
