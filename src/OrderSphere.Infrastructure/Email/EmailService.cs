using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Options;
using OrderSphere.Application.Abstraction;
using OrderSphere.Domain.Configuration;
using System.Net.Mail;

namespace OrderSphere.Infrastructure.Email;
public sealed class EmailService(IOptions<MailConfiguration> options) : IEmailService
{
    private readonly MailConfiguration mailConfiguration = options.Value;

    public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
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
        var html = @"
            <html>
              <body style='margin:0; padding:0; background-color:#F8FAFC; font-family:Arial, sans-serif;'>
            
                <table width='100%' cellpadding='0' cellspacing='0' style='padding:20px 0;'>
                  <tr>
                    <td align='center'>
            
                      <!-- Container -->
                      <table width='600' cellpadding='0' cellspacing='0' style='background:#FFFFFF; border-radius:10px; padding:30px;'>
            
                        <!-- Header -->
                        <tr>
                          <td align='center' style='padding-bottom:20px;'>
                            <h2 style='margin:0; color:#6366F1;'>OrderSphere</h2>
                          </td>
                        </tr>
            
                        <!-- Content -->
                        <tr>
                          <td style='color:#0F172A;'>
            
                            <h3 style='margin-top:0;'>E-Mail bestätigen</h3>
            
                            <p>
                              Vielen Dank für deine Registrierung bei <strong>OrderSphere</strong>.
                            </p>
            
                            <p>
                              Bitte bestätige deine E-Mail-Adresse, um dein Konto zu aktivieren:
                            </p>
            
                            <!-- Button -->
                            <div style='text-align:center; margin:30px 0;'>
                              <a href='{{CONFIRMATION_LINK}}'
                                 style='background-color:#6366F1; color:#FFFFFF; padding:12px 24px; text-decoration:none; border-radius:6px; display:inline-block; font-weight:bold;'>
                                E-Mail bestätigen
                              </a>
                            </div>
            
                            <p style='font-size:14px; color:#475569;'>
                              Falls der Button nicht funktioniert, kannst du auch diesen Link verwenden:
                            </p>
            
                            <p style='word-break:break-all; font-size:13px; color:#3B82F6;'>
                              {{CONFIRMATION_LINK}}
                            </p>
            
                          </td>
                        </tr>
            
                        <!-- Footer -->
                        <tr>
                          <td style='padding-top:30px; font-size:12px; color:#94A3B8; text-align:center;'>
            
                            <p style='margin:0;'>
                              © " + DateTime.Now.Year + @" OrderSphere
                            </p>
            
                            <p style='margin:5px 0 0 0;'>
                              Wenn du diese Anfrage nicht gestellt hast, kannst du diese E-Mail ignorieren.
                            </p>
            
                          </td>
                        </tr>
            
                      </table>
            
                    </td>
                  </tr>
                </table>
            
              </body>
            </html>
            ";

        return html.Replace("{{CONFIRMATION_LINK}}", link);
    }
}
