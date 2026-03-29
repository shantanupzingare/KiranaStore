using Microsoft.EntityFrameworkCore;
using KiranaStore.Application.DTOs;
using KiranaStore.Application.Interfaces;
using KiranaStore.Application.Mapping;
using KiranaStore.Domain.Interfaces;
using KiranaStore.Persistence.Context;
using KiranaStore.Shared.Helpers;
using KiranaStore.Shared.Responses;

// Explicit aliases prevent any ambiguity if other namespaces are added later
using DomainOrder     = KiranaStore.Domain.Entities.Order;
using DomainOrderItem = KiranaStore.Domain.Entities.OrderItem;
using DomainPayment   = KiranaStore.Domain.Entities.Payment;
using DomainDiscount  = KiranaStore.Domain.Entities.Discount;

namespace KiranaStore.Application.Services;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork    _uow;
    private readonly AppDbContext   _db;
    private readonly IEmailService    _email;
    private readonly IWhatsAppService _whatsapp;
    private readonly IInvoiceService  _invoice;

    public OrderService(IUnitOfWork uow, AppDbContext db,
        IEmailService email, IWhatsAppService whatsapp, IInvoiceService invoice)
    {
        _uow = uow; _db = db;
        _email = email; _whatsapp = whatsapp; _invoice = invoice;
    }

    public async Task<ApiResponse<PagedResponse<OrderDto>>> GetAllAsync(
        int page, int size, DateTime? from, DateTime? to)
    {
        var q = _db.Orders
            .Include(o => o.Customer)
            .Include(o => o.User)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .Where(o => !o.IsDeleted);

        if (from.HasValue) q = q.Where(o => o.OrderDate >= from.Value);
        if (to.HasValue)   q = q.Where(o => o.OrderDate <= to.Value.AddDays(1));

        q = q.OrderByDescending(o => o.OrderDate);
        var total = await q.CountAsync();
        var items = await q.Skip((page - 1) * size).Take(size).ToListAsync();

        return ApiResponse<PagedResponse<OrderDto>>.Ok(new PagedResponse<OrderDto>
        {
            Data       = items.Select(o => o.ToDto()),
            TotalCount = total,
            Page       = page,
            PageSize   = size
        });
    }

    public async Task<ApiResponse<OrderDto>> GetByIdAsync(int id)
    {
        var o = await _db.Orders
            .Include(o => o.Customer)
            .Include(o => o.User)
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id && !o.IsDeleted);

        if (o == null) return ApiResponse<OrderDto>.Fail("Order not found");
        return ApiResponse<OrderDto>.Ok(o.ToDto());
    }

    public async Task<ApiResponse<OrderDto>> CreateAsync(CreateOrderDto dto, int userId)
    {
        // ── Validate stock ───────────────────────────────────────────────────
        foreach (var item in dto.Items)
        {
            var product = await _uow.Products.GetByIdAsync(item.ProductId);
            if (product == null)
                return ApiResponse<OrderDto>.Fail($"Product ID {item.ProductId} not found");
            if (product.Stock < item.Quantity)
                return ApiResponse<OrderDto>.Fail(
                    $"'{product.Name}': only {product.Stock} in stock, requested {item.Quantity}");
        }

        // ── Resolve discount ─────────────────────────────────────────────────
        decimal discountAmount = dto.ManualDiscount;
        if (!string.IsNullOrEmpty(dto.DiscountCode))
        {
            var disc = await _db.Discounts.FirstOrDefaultAsync(d =>
                d.Code == dto.DiscountCode && d.IsActive && !d.IsDeleted &&
                (d.ExpiryDate == null || d.ExpiryDate > DateTime.UtcNow) &&
                d.UsedCount < d.UsageLimit);

            if (disc != null)
            {
                var raw = dto.Items.Sum(i => i.Quantity * i.UnitPrice);
                discountAmount += disc.Type == "Percentage"
                    ? Math.Min(raw * disc.Value / 100m, disc.MaxDiscount ?? decimal.MaxValue)
                    : disc.Value;
                disc.UsedCount++;
                await _uow.Discounts.UpdateAsync(disc);
            }
        }

        // ── Build order ──────────────────────────────────────────────────────
        var order = new DomainOrder
        {
            InvoiceNumber = InvoiceHelper.Generate(),
            CustomerId    = dto.CustomerId,
            UserId        = userId,
            PaymentMethod = dto.PaymentMethod,
            Notes         = dto.Notes,
            PaymentStatus = dto.PaymentMethod == "Razorpay" ? "Pending" : "Paid"
        };

        decimal subTotal = 0m, gstTotal = 0m;

        foreach (var item in dto.Items)
        {
            var product  = await _uow.Products.GetByIdAsync(item.ProductId);
            var lineBase = item.Quantity * item.UnitPrice;
            var lineGst  = lineBase * product!.GSTRate / 100m;
            var lineDisc = item.DiscountAmount;
            var lineTotal = lineBase + lineGst - lineDisc;

            subTotal += lineBase;
            gstTotal += lineGst;

            order.Items.Add(new DomainOrderItem
            {
                ProductId      = item.ProductId,
                ProductName    = product.Name,
                UnitPrice      = item.UnitPrice,
                Quantity       = item.Quantity,
                GSTRate        = product.GSTRate,
                GSTAmount      = lineGst,
                DiscountAmount = lineDisc,
                Total          = lineTotal
            });

            // Deduct stock
            product.Stock    -= item.Quantity;
            product.UpdatedAt = DateTime.UtcNow;
            await _uow.Products.UpdateAsync(product);
        }

        order.SubTotal       = subTotal;
        order.DiscountAmount = discountAmount;
        order.GSTAmount      = gstTotal;
        order.TotalAmount    = subTotal + gstTotal - discountAmount;

        await _uow.Orders.AddAsync(order);
        await _uow.SaveChangesAsync();

        // ── Record payment for non-Razorpay methods ─────────────────────────
        if (dto.PaymentMethod != "Razorpay")
        {
            await _uow.Payments.AddAsync(new DomainPayment
            {
                OrderId = order.Id,
                Amount  = order.TotalAmount,
                Method  = dto.PaymentMethod,
                Status  = "Success"
            });
            await _uow.SaveChangesAsync();
        }

        // ── Auto-send notifications ──────────────────────────────────────────
        var saved = await GetByIdAsync(order.Id);
        if (saved.Data != null && dto.CustomerId.HasValue)
        {
            var customer = await _uow.Customers.GetByIdAsync(dto.CustomerId.Value);
            if (customer != null)
            {
                // Email
                if (!string.IsNullOrEmpty(customer.Email))
                {
                    var pdf  = await _invoice.GeneratePdfAsync(saved.Data, "Kirana Store", "", "");
                    var sent = await _email.SendInvoiceAsync(
                        customer.Email, customer.Name, order.InvoiceNumber, pdf);
                    if (sent)
                    {
                        order.EmailSent   = true;
                        order.UpdatedAt   = DateTime.UtcNow;
                        await _uow.Orders.UpdateAsync(order);
                        await _uow.SaveChangesAsync();
                    }
                }

                // WhatsApp
                var waNumber = customer.WhatsAppNumber ?? customer.Phone;
                if (!string.IsNullOrEmpty(waNumber))
                {
                    var url  = $"/api/orders/{order.Id}/invoice";
                    var sent = await _whatsapp.SendInvoiceAsync(
                        waNumber, customer.Name, order.InvoiceNumber, url);
                    if (sent)
                    {
                        order.WhatsAppSent = true;
                        order.UpdatedAt    = DateTime.UtcNow;
                        await _uow.Orders.UpdateAsync(order);
                        await _uow.SaveChangesAsync();
                    }
                }
            }
        }

        return saved;
    }

    public async Task<ApiResponse<bool>> CancelAsync(int id)
    {
        var o = await _uow.Orders.GetByIdAsync(id);
        if (o == null) return ApiResponse<bool>.Fail("Order not found");
        o.OrderStatus = "Cancelled";
        o.IsDeleted   = true;
        o.UpdatedAt   = DateTime.UtcNow;
        await _uow.Orders.UpdateAsync(o);
        await _uow.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true);
    }

    public async Task<ApiResponse<byte[]>> GenerateInvoicePdfAsync(int id)
    {
        var res = await GetByIdAsync(id);
        if (!res.Success || res.Data == null)
            return ApiResponse<byte[]>.Fail("Order not found");

        var pdf = await _invoice.GeneratePdfAsync(
            res.Data, "Kirana Store", "123 Main Street", "22AAAAA0000A1Z5");
        return ApiResponse<byte[]>.Ok(pdf);
    }
}
