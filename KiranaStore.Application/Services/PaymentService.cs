using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using KiranaStore.Application.DTOs;
using KiranaStore.Application.Interfaces;
using KiranaStore.Domain.Interfaces;
using KiranaStore.Persistence.Context;
using KiranaStore.Shared.Responses;

// Alias our own Payment entity to avoid any future naming collisions
using DomainPayment = KiranaStore.Domain.Entities.Payment;

namespace KiranaStore.Application.Services;

/// <summary>
/// Razorpay integration via direct REST calls — no SDK, no .NET Framework dependency.
/// </summary>
public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _uow;
    private readonly AppDbContext _db;
    private readonly IConfiguration _config;
    private readonly IHttpClientFactory _http;

    public PaymentService(IUnitOfWork uow, AppDbContext db, IConfiguration config, IHttpClientFactory http)
    {
        _uow = uow; _db = db; _config = config; _http = http;
    }

    public async Task<ApiResponse<RazorpayOrderDto>> CreateRazorpayOrderAsync(int orderId)
    {
        var order = await _uow.Orders.GetByIdAsync(orderId);
        if (order == null)
            return ApiResponse<RazorpayOrderDto>.Fail("Order not found");

        var keyId     = _config["Razorpay:KeyId"]     ?? "";
        var keySecret = _config["Razorpay:KeySecret"] ?? "";

        if (string.IsNullOrEmpty(keyId) || keyId.StartsWith("rzp_test_YOUR"))
            return ApiResponse<RazorpayOrderDto>.Fail("Razorpay not configured. Set KeyId and KeySecret in appsettings.json.");

        try
        {
            var client = _http.CreateClient();
            var credentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{keyId}:{keySecret}"));
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

            var body = JsonSerializer.Serialize(new
            {
                amount   = (long)(order.TotalAmount * 100), // paise
                currency = "INR",
                receipt  = order.InvoiceNumber
            });

            var response = await client.PostAsync(
                "https://api.razorpay.com/v1/orders",
                new StringContent(body, Encoding.UTF8, "application/json"));

            if (!response.IsSuccessStatusCode)
            {
                var err = await response.Content.ReadAsStringAsync();
                return ApiResponse<RazorpayOrderDto>.Fail($"Razorpay API error: {err}");
            }

            var json    = await response.Content.ReadAsStringAsync();
            var doc     = JsonDocument.Parse(json);
            var rzpId   = doc.RootElement.GetProperty("id").GetString() ?? "";

            order.RazorpayOrderId = rzpId;
            await _uow.Orders.UpdateAsync(order);
            await _uow.SaveChangesAsync();

            return ApiResponse<RazorpayOrderDto>.Ok(new RazorpayOrderDto
            {
                OrderId  = rzpId,
                Amount   = order.TotalAmount,
                Currency = "INR",
                Key      = keyId
            });
        }
        catch (Exception ex)
        {
            return ApiResponse<RazorpayOrderDto>.Fail($"Razorpay error: {ex.Message}");
        }
    }

    public async Task<ApiResponse<bool>> VerifyPaymentAsync(VerifyPaymentDto dto)
    {
        var keySecret = _config["Razorpay:KeySecret"] ?? "";

        // Verify HMAC-SHA256 signature
        var payload   = $"{dto.RazorpayOrderId}|{dto.RazorpayPaymentId}";
        var signature = ComputeHmac(payload, keySecret);

        if (signature != dto.RazorpaySignature)
            return ApiResponse<bool>.Fail("Payment signature verification failed. Possible tampering detected.");

        var order = await _uow.Orders.GetByIdAsync(dto.OrderId);
        if (order == null)
            return ApiResponse<bool>.Fail("Order not found");

        order.PaymentStatus       = "Paid";
        order.RazorpayPaymentId   = dto.RazorpayPaymentId;
        order.UpdatedAt           = DateTime.UtcNow;
        await _uow.Orders.UpdateAsync(order);

        // Record payment (using full namespace to be explicit)
        var payment = new DomainPayment
        {
            OrderId           = order.Id,
            Amount            = order.TotalAmount,
            Method            = "Razorpay",
            Status            = "Success",
            RazorpayOrderId   = dto.RazorpayOrderId,
            RazorpayPaymentId = dto.RazorpayPaymentId,
            RazorpaySignature = dto.RazorpaySignature
        };
        await _uow.Payments.AddAsync(payment);
        await _uow.SaveChangesAsync();

        return ApiResponse<bool>.Ok(true, "Payment verified successfully");
    }

    private static string ComputeHmac(string data, string key)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(data));
        return Convert.ToHexString(hash).ToLower();
    }
}
