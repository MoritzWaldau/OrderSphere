namespace OrderSphere.Application.Abstraction;

public interface IEmailService
{
    Task SendLinkAsync(string toEmail, string resetLink);
}