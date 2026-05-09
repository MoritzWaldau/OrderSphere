using OrderSphere.Application.Models;

namespace OrderSphere.Application.Abstraction;

public interface IEmailService
{
    Task SendLinkAsync(string toEmail, string resetLink);
    Task SendOrderConfirmationAsync(string toEmail, OrderConfirmationData data);
}
