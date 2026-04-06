using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Options;
using OrderSphere.Application.Abstraction;
using OrderSphere.Domain.Configuration;

namespace OrderSphere.Infrastructure.Email;
public sealed class EmailService(IOptions<MailConfiguration> options) : IEmailService
{
    private readonly MailConfiguration mailConfiguration = options.Value;

    public async Task SendLinkAsync(string toEmail, string resetLink)
    {
        var mailClient = new EmailClient(mailConfiguration.ConnectionString);

        var message = new EmailMessage(
            senderAddress: mailConfiguration.SenderAddress,
            recipients: new EmailRecipients([new EmailAddress(toEmail)]),
            content: new EmailContent("Subject")
            {
                PlainText = @"Hello world via email.",
                Html = GetHtmlTemplate(resetLink)
            }
        );


        var result = await mailClient.SendAsync(
            WaitUntil.Completed,
            message);
    }


    private static string GetHtmlTemplate(string link)
    {
        var html = @$"
        <html>
          <body style='margin:0; padding:0; background-color:#F8FAFC; font-family:Arial, sans-serif;'>
            <table width='100%' cellpadding='0' cellspacing='0' style='padding:20px 0;'>
              <tr>
                <td align='center'>
                  <table width='600' cellpadding='0' cellspacing='0' style='background:#FFFFFF; border-radius:10px; padding:30px;'>
                    <tr>
                      <td align='center' style='padding-bottom:20px;'>
                        <h2 style='margin:0; color:#6366F1;'>OrderSphere</h2>
                      </td>
                    </tr>
                    <tr>
                      <td style='text-align:center; padding:20px 0;'>
                        <p>Bitte bestätige deine E-Mail-Adresse:</p>
                        <a href='{link}' style='display:inline-block; padding:12px 30px; background-color:#6366F1; color:white; text-decoration:none; border-radius:5px; margin:20px 0;'>E-Mail bestätigen</a>
                        <p style='color:#666; font-size:12px;'>Oder kopiere diesen Link: {link}</p>
                      </td>
                    </tr>
                  </table>
                </td>
              </tr>
            </table>
          </body>
        </html>";
        return html;
    }
}

