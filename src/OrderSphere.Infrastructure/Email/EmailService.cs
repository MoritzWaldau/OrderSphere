using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Options;
using OrderSphere.Application.Abstraction;
using OrderSphere.Application.Models;
using OrderSphere.Domain.Configuration;
using System.Globalization;
using System.Text;

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

    public async Task SendOrderConfirmationAsync(string toEmail, OrderConfirmationData data)
    {
        var mailClient = new EmailClient(mailConfiguration.ConnectionString);

        var subject = $"Deine OrderSphere-Bestellung {data.OrderId} – Tracking {data.TrackingNumber}";

        var message = new EmailMessage(
            senderAddress: mailConfiguration.SenderAddress,
            recipients: new EmailRecipients([new EmailAddress(toEmail)]),
            content: new EmailContent(subject)
            {
                PlainText = BuildPlainText(data),
                Html = BuildOrderConfirmationHtml(data)
            }
        );

        await mailClient.SendAsync(WaitUntil.Completed, message);
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

    private static string BuildPlainText(OrderConfirmationData data)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Vielen Dank für deine Bestellung bei OrderSphere!");
        sb.AppendLine();
        sb.AppendLine($"Bestellnummer: {data.OrderId}");
        sb.AppendLine($"Tracking-Nummer: {data.TrackingNumber}");
        sb.AppendLine();
        sb.AppendLine("Lieferadresse:");
        sb.AppendLine($"  {data.ShippingAddress.FirstName} {data.ShippingAddress.LastName}");
        sb.AppendLine($"  {data.ShippingAddress.Street}");
        sb.AppendLine($"  {data.ShippingAddress.PostalCode} {data.ShippingAddress.City}");
        sb.AppendLine($"  {data.ShippingAddress.Country}");
        sb.AppendLine();
        sb.AppendLine("Artikel:");
        foreach (var item in data.Items)
        {
            sb.AppendLine($"  {item.Quantity}x {item.ProductName} – {item.Price.ToString("C", CultureInfo.GetCultureInfo("de-DE"))}");
        }
        sb.AppendLine();
        sb.AppendLine($"Gesamt: {data.Total.ToString("C", CultureInfo.GetCultureInfo("de-DE"))}");
        return sb.ToString();
    }

    private static string BuildOrderConfirmationHtml(OrderConfirmationData data)
    {
        var de = CultureInfo.GetCultureInfo("de-DE");

        var itemRows = new StringBuilder();
        foreach (var item in data.Items)
        {
            itemRows.Append($@"
                <tr>
                    <td style='padding:8px; border-bottom:1px solid #E5E7EB;'>{item.ProductName}</td>
                    <td style='padding:8px; border-bottom:1px solid #E5E7EB; text-align:center;'>{item.Quantity}</td>
                    <td style='padding:8px; border-bottom:1px solid #E5E7EB; text-align:right;'>{item.Price.ToString("C", de)}</td>
                </tr>");
        }

        return $@"
        <html>
          <body style='margin:0; padding:0; background-color:#F8FAFC; font-family:Arial, sans-serif; color:#111827;'>
            <table width='100%' cellpadding='0' cellspacing='0' style='padding:20px 0;'>
              <tr>
                <td align='center'>
                  <table width='600' cellpadding='0' cellspacing='0' style='background:#FFFFFF; border-radius:12px; padding:30px;'>
                    <tr>
                      <td align='center' style='padding-bottom:20px;'>
                        <h2 style='margin:0; color:#6366F1;'>OrderSphere</h2>
                      </td>
                    </tr>
                    <tr>
                      <td>
                        <h3 style='margin:0 0 12px 0;'>Vielen Dank für deine Bestellung!</h3>
                        <p style='margin:0 0 4px 0;'>Bestellnummer: <strong>{data.OrderId}</strong></p>
                        <p style='margin:0 0 20px 0;'>Tracking-Nummer: <strong>{data.TrackingNumber}</strong></p>
                      </td>
                    </tr>
                    <tr>
                      <td>
                        <h4 style='margin:20px 0 8px 0; color:#374151;'>Lieferadresse</h4>
                        <p style='margin:0; line-height:1.5;'>
                          {data.ShippingAddress.FirstName} {data.ShippingAddress.LastName}<br/>
                          {data.ShippingAddress.Street}<br/>
                          {data.ShippingAddress.PostalCode} {data.ShippingAddress.City}<br/>
                          {data.ShippingAddress.Country}
                        </p>
                      </td>
                    </tr>
                    <tr>
                      <td>
                        <h4 style='margin:20px 0 8px 0; color:#374151;'>Artikel</h4>
                        <table width='100%' cellpadding='0' cellspacing='0' style='border-collapse:collapse;'>
                          <thead>
                            <tr style='background:#F3F4F6;'>
                              <th style='padding:8px; text-align:left;'>Produkt</th>
                              <th style='padding:8px; text-align:center;'>Menge</th>
                              <th style='padding:8px; text-align:right;'>Preis</th>
                            </tr>
                          </thead>
                          <tbody>
                            {itemRows}
                          </tbody>
                          <tfoot>
                            <tr>
                              <td colspan='2' style='padding:12px 8px; text-align:right; font-weight:bold;'>Gesamt:</td>
                              <td style='padding:12px 8px; text-align:right; font-weight:bold; color:#6366F1;'>{data.Total.ToString("C", de)}</td>
                            </tr>
                          </tfoot>
                        </table>
                      </td>
                    </tr>
                  </table>
                </td>
              </tr>
            </table>
          </body>
        </html>";
    }
}
