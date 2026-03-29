using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using KiranaStore.Application.Interfaces;

namespace KiranaStore.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _log;

    public EmailService(IConfiguration config, ILogger<EmailService> log)
    { _config = config; _log = log; }

    public async Task<bool> SendInvoiceAsync(string toEmail, string customerName,
        string invoiceNumber, byte[] pdfBytes)
    {
        try
        {
            var smtp     = _config["Email:SmtpHost"]     ?? "smtp.gmail.com";
            var port     = int.Parse(_config["Email:SmtpPort"] ?? "587");
            var user     = _config["Email:Username"]     ?? "";
            var pass     = _config["Email:Password"]     ?? "";
            var from     = _config["Email:FromEmail"]    ?? user;
            var store    = _config["Store:Name"]         ?? "Kirana Store";

            using var client = new SmtpClient(smtp, port)
            {
                Credentials  = new NetworkCredential(user, pass),
                EnableSsl    = true,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            var body = $@"
<!DOCTYPE html>
<html>
<head>
  <meta charset='utf-8'>
  <style>
    body {{ font-family: 'Segoe UI', Arial, sans-serif; background: #f0f4f8; margin: 0; padding: 0; }}
    .container {{ max-width: 600px; margin: 30px auto; background: #fff; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 24px rgba(0,0,0,0.08); }}
    .header {{ background: linear-gradient(135deg, #6366f1, #8b5cf6); padding: 32px; text-align: center; }}
    .header h1 {{ color: #fff; margin: 0; font-size: 24px; }}
    .header p {{ color: rgba(255,255,255,0.85); margin: 8px 0 0; }}
    .body {{ padding: 32px; }}
    .invoice-badge {{ background: #f0f4ff; border: 1px solid #c7d2fe; border-radius: 8px; padding: 16px; margin: 20px 0; text-align: center; }}
    .invoice-badge span {{ font-size: 22px; font-weight: 700; color: #4f46e5; }}
    .cta {{ text-align: center; margin: 24px 0; }}
    .cta a {{ background: linear-gradient(135deg, #6366f1, #8b5cf6); color: #fff; padding: 14px 32px; border-radius: 8px; text-decoration: none; font-weight: 600; font-size: 15px; }}
    .footer {{ background: #f8fafc; padding: 20px; text-align: center; color: #94a3b8; font-size: 12px; border-top: 1px solid #e2e8f0; }}
  </style>
</head>
<body>
  <div class='container'>
    <div class='header'>
      <h1>🛒 {store}</h1>
      <p>Your invoice is ready!</p>
    </div>
    <div class='body'>
      <p>Hello <strong>{customerName}</strong>,</p>
      <p>Thank you for shopping with us! Your invoice is attached to this email.</p>
      <div class='invoice-badge'>
        <div style='color:#64748b;font-size:13px;margin-bottom:4px;'>Invoice Number</div>
        <span>{invoiceNumber}</span>
      </div>
      <p style='color:#64748b;'>Please find your invoice PDF attached. Keep it safe for your records.</p>
    </div>
    <div class='footer'>
      <p>© {DateTime.UtcNow.Year} {store}. All rights reserved.</p>
      <p>This is an automated email. Please do not reply.</p>
    </div>
  </div>
</body>
</html>";

            using var msg = new MailMessage
            {
                From       = new MailAddress(from, store),
                Subject    = $"Your Invoice {invoiceNumber} from {store}",
                Body       = body,
                IsBodyHtml = true
            };
            msg.To.Add(toEmail);
            msg.Attachments.Add(new Attachment(new MemoryStream(pdfBytes), $"{invoiceNumber}.pdf", "application/pdf"));

            await client.SendMailAsync(msg);
            _log.LogInformation("Invoice email sent to {Email} for {Invoice}", toEmail, invoiceNumber);
            return true;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "Failed to send invoice email to {Email}", toEmail);
            return false;
        }
    }
}
