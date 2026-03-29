using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using KiranaStore.Application.Interfaces;

namespace KiranaStore.Infrastructure.Services;

public class WhatsAppService : IWhatsAppService
{
    private readonly IConfiguration _config;
    private readonly ILogger<WhatsAppService> _log;

    public WhatsAppService(IConfiguration config, ILogger<WhatsAppService> log)
    { _config = config; _log = log; }

    public async Task<bool> SendInvoiceAsync(string phone, string customerName,
        string invoiceNumber, string invoiceUrl)
    {
        var accountSid = _config["Twilio:AccountSid"] ?? "";
        var authToken  = _config["Twilio:AuthToken"]  ?? "";
        var from       = _config["Twilio:WhatsAppFrom"] ?? "whatsapp:+14155238886";
        var store      = _config["Store:Name"] ?? "Kirana Store";

        if (string.IsNullOrEmpty(accountSid) || accountSid == "YOUR_TWILIO_SID")
        {
            _log.LogWarning("Twilio not configured — WhatsApp skipped for {Phone}", phone);
            return false;
        }

        try
        {
            // Normalize phone number for WhatsApp
            var toNumber = phone.StartsWith("+") ? phone : $"+91{phone.TrimStart('0')}";
            var to       = $"whatsapp:{toNumber}";

            var message = $"🛒 *{store}*\n\n" +
                          $"Hello {customerName}! 👋\n\n" +
                          $"Your invoice *{invoiceNumber}* is ready.\n\n" +
                          $"📄 Download here: {invoiceUrl}\n\n" +
                          $"Thank you for shopping with us! 🙏";

            // Use Twilio REST API directly (no SDK to keep dependencies light)
            using var http = new HttpClient();
            http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
                "Basic", Convert.ToBase64String(
                    System.Text.Encoding.ASCII.GetBytes($"{accountSid}:{authToken}")));

            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string,string>("From",    from),
                new KeyValuePair<string,string>("To",      to),
                new KeyValuePair<string,string>("Body",    message)
            });

            var response = await http.PostAsync(
                $"https://api.twilio.com/2010-04-01/Accounts/{accountSid}/Messages.json", content);

            if (response.IsSuccessStatusCode)
            {
                _log.LogInformation("WhatsApp sent to {Phone} for {Invoice}", phone, invoiceNumber);
                return true;
            }

            var err = await response.Content.ReadAsStringAsync();
            _log.LogError("WhatsApp failed for {Phone}: {Error}", phone, err);
            return false;
        }
        catch (Exception ex)
        {
            _log.LogError(ex, "WhatsApp exception for {Phone}", phone);
            return false;
        }
    }
}
