using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text;

namespace OrderSphere.Ordering.Infrastructure.Email;

public sealed class OrderingEmailService(IOptions<OrderingMailConfiguration> options) : IOrderingEmailService
{
    private readonly OrderingMailConfiguration _config = options.Value;

    public async Task SendOrderConfirmationAsync(string toEmail, OrderingConfirmationData data)
    {
        var mailClient = new EmailClient(_config.ConnectionString);

        var subject = $"Deine OrderSphere-Bestellung {data.OrderId} – Tracking {data.TrackingNumber}";

        var message = new EmailMessage(
            senderAddress: _config.SenderAddress,
            recipients: new EmailRecipients([new EmailAddress(toEmail)]),
            content: new EmailContent(subject)
            {
                PlainText = BuildPlainText(data),
                Html = BuildHtml(data)
            });

        await mailClient.SendAsync(WaitUntil.Completed, message);
    }

    private static string BuildPlainText(OrderingConfirmationData data)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Vielen Dank für deine Bestellung bei OrderSphere!");
        sb.AppendLine();
        sb.AppendLine($"Bestellnummer: {data.OrderId}");
        sb.AppendLine($"Tracking-Nummer: {data.TrackingNumber}");
        sb.AppendLine();
        sb.AppendLine("Lieferadresse:");
        sb.AppendLine($"  {data.ShippingFirstName} {data.ShippingLastName}");
        sb.AppendLine($"  {data.ShippingStreet}");
        sb.AppendLine($"  {data.ShippingPostalCode} {data.ShippingCity}");
        sb.AppendLine($"  {data.ShippingCountry}");
        sb.AppendLine();
        sb.AppendLine("Artikel:");
        foreach (var item in data.Items)
            sb.AppendLine($"  {item.Quantity}x {item.ProductName} – {item.Price.ToString("C", CultureInfo.GetCultureInfo("de-DE"))}");
        sb.AppendLine();
        sb.AppendLine($"Gesamt: {data.Total.ToString("C", CultureInfo.GetCultureInfo("de-DE"))}");
        return sb.ToString();
    }

    private static string BuildHtml(OrderingConfirmationData data)
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
        <html><body style='font-family:Arial,sans-serif;'>
          <h2 style='color:#6366F1;'>OrderSphere</h2>
          <h3>Vielen Dank für deine Bestellung!</h3>
          <p>Bestellnummer: <strong>{data.OrderId}</strong></p>
          <p>Tracking-Nummer: <strong>{data.TrackingNumber}</strong></p>
          <h4>Lieferadresse</h4>
          <p>{data.ShippingFirstName} {data.ShippingLastName}<br/>
             {data.ShippingStreet}<br/>
             {data.ShippingPostalCode} {data.ShippingCity}<br/>
             {data.ShippingCountry}</p>
          <h4>Artikel</h4>
          <table width='100%' style='border-collapse:collapse;'>
            <thead><tr style='background:#F3F4F6;'>
              <th style='padding:8px; text-align:left;'>Produkt</th>
              <th style='padding:8px; text-align:center;'>Menge</th>
              <th style='padding:8px; text-align:right;'>Preis</th>
            </tr></thead>
            <tbody>{itemRows}</tbody>
            <tfoot><tr>
              <td colspan='2' style='padding:12px 8px; text-align:right; font-weight:bold;'>Gesamt:</td>
              <td style='padding:12px 8px; text-align:right; font-weight:bold; color:#6366F1;'>{data.Total.ToString("C", de)}</td>
            </tr></tfoot>
          </table>
        </body></html>";
    }
}
