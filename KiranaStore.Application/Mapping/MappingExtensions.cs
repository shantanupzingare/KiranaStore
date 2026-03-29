using KiranaStore.Application.DTOs;
using KiranaStore.Domain.Entities;

namespace KiranaStore.Application.Mapping;

public static class MappingExtensions
{
    public static UserDto ToDto(this User u) => new()
    {
        Id = u.Id, FullName = u.FullName, Email = u.Email,
        Phone = u.Phone, Role = u.Role, IsActive = u.IsActive, CreatedAt = u.CreatedAt
    };

    public static ProductDto ToDto(this Product p) => new()
    {
        Id = p.Id, Name = p.Name, SKU = p.SKU, Barcode = p.Barcode,
        CategoryId = p.CategoryId, CategoryName = p.Category?.Name ?? string.Empty,
        PurchasePrice = p.PurchasePrice, SellingPrice = p.SellingPrice, MRP = p.MRP,
        Stock = p.Stock, MinStockLevel = p.MinStockLevel, Unit = p.Unit,
        GSTRate = p.GSTRate, IsActive = p.IsActive, ImageUrl = p.ImageUrl,
        SupplierId = p.SupplierId, SupplierName = p.Supplier?.Name, CreatedAt = p.CreatedAt
    };

    public static CategoryDto ToDto(this Category c) => new()
    {
        Id = c.Id, Name = c.Name, ProductCount = c.Products?.Count(p => !p.IsDeleted) ?? 0
    };

    public static CustomerDto ToDto(this Customer c) => new()
    {
        Id = c.Id, Name = c.Name, Phone = c.Phone, WhatsAppNumber = c.WhatsAppNumber,
        Email = c.Email, Address = c.Address, GSTIN = c.GSTIN, LoyaltyPoints = c.LoyaltyPoints,
        TotalOrders = c.Orders?.Count(o => !o.IsDeleted) ?? 0,
        TotalSpent = c.Orders?.Where(o => !o.IsDeleted).Sum(o => o.TotalAmount) ?? 0,
        CreatedAt = c.CreatedAt
    };

    public static SupplierDto ToDto(this Supplier s) => new()
    {
        Id = s.Id, Name = s.Name, Phone = s.Phone, Email = s.Email,
        Address = s.Address, GSTIN = s.GSTIN,
        ProductCount = s.Products?.Count(p => !p.IsDeleted) ?? 0, CreatedAt = s.CreatedAt
    };

    public static OrderItemDto ToDto(this OrderItem i) => new()
    {
        ProductId = i.ProductId, ProductName = i.ProductName, UnitPrice = i.UnitPrice,
        Quantity = i.Quantity, GSTRate = i.GSTRate, GSTAmount = i.GSTAmount,
        DiscountAmount = i.DiscountAmount, Total = i.Total
    };

    public static OrderDto ToDto(this Order o) => new()
    {
        Id = o.Id, InvoiceNumber = o.InvoiceNumber,
        CustomerId = o.CustomerId, CustomerName = o.Customer?.Name ?? "Walk-in",
        UserName = o.User?.FullName ?? string.Empty,
        OrderDate = o.OrderDate, SubTotal = o.SubTotal, DiscountAmount = o.DiscountAmount,
        GSTAmount = o.GSTAmount, TotalAmount = o.TotalAmount,
        PaymentMethod = o.PaymentMethod, PaymentStatus = o.PaymentStatus,
        OrderStatus = o.OrderStatus, EmailSent = o.EmailSent, WhatsAppSent = o.WhatsAppSent,
        Items = o.Items?.Select(i => i.ToDto()).ToList() ?? new()
    };

    public static DiscountDto ToDto(this Discount d) => new()
    {
        Id = d.Id, Code = d.Code, Type = d.Type, Value = d.Value,
        IsActive = d.IsActive, UsedCount = d.UsedCount
    };
}
