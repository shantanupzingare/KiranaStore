namespace KiranaStore.Application.DTOs;

// ── Auth ─────────────────────────────────────────────────────────────────────
public record LoginRequest(string Email, string Password);
public record LoginResponse(string Token, string RefreshToken, DateTime Expiry, UserDto User);
public record RefreshRequest(string Token, string RefreshToken);

// ── User ─────────────────────────────────────────────────────────────────────
public class UserDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
public class CreateUserDto
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Role { get; set; } = "Staff";
}

// ── Product ───────────────────────────────────────────────────────────────────
public class ProductDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal MRP { get; set; }
    public int Stock { get; set; }
    public int MinStockLevel { get; set; }
    public string Unit { get; set; } = string.Empty;
    public decimal GSTRate { get; set; }
    public bool IsActive { get; set; }
    public string? ImageUrl { get; set; }
    public int? SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public DateTime CreatedAt { get; set; }
}
public class CreateProductDto
{
    public string Name { get; set; } = string.Empty;
    public string SKU { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public int CategoryId { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal SellingPrice { get; set; }
    public decimal MRP { get; set; }
    public int Stock { get; set; }
    public int MinStockLevel { get; set; } = 10;
    public string Unit { get; set; } = "pcs";
    public decimal GSTRate { get; set; } = 18;
    public int? SupplierId { get; set; }
}
public class UpdateProductDto : CreateProductDto { public bool IsActive { get; set; } = true; }

// ── Category ─────────────────────────────────────────────────────────────────
public class CategoryDto { public int Id { get; set; } public string Name { get; set; } = string.Empty; public int ProductCount { get; set; } }
public class CreateCategoryDto { public string Name { get; set; } = string.Empty; public string? Description { get; set; } }

// ── Customer ─────────────────────────────────────────────────────────────────
public class CustomerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? WhatsAppNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? GSTIN { get; set; }
    public decimal LoyaltyPoints { get; set; }
    public int TotalOrders { get; set; }
    public decimal TotalSpent { get; set; }
    public DateTime CreatedAt { get; set; }
}
public class CreateCustomerDto
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? WhatsAppNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? GSTIN { get; set; }
}

// ── Supplier ─────────────────────────────────────────────────────────────────
public class SupplierDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? GSTIN { get; set; }
    public int ProductCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
public class CreateSupplierDto
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? GSTIN { get; set; }
}

// ── Order ─────────────────────────────────────────────────────────────────────
public class OrderItemDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal GSTRate { get; set; }
    public decimal GSTAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }
}
public class OrderDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal GSTAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string OrderStatus { get; set; } = string.Empty;
    public bool EmailSent { get; set; }
    public bool WhatsAppSent { get; set; }
    public List<OrderItemDto> Items { get; set; } = new();
}
public class CreateOrderItemDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
}
public class CreateOrderDto
{
    public int? CustomerId { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public string? DiscountCode { get; set; }
    public decimal ManualDiscount { get; set; }
    public string? Notes { get; set; }
    public List<CreateOrderItemDto> Items { get; set; } = new();
}

// ── Payment ───────────────────────────────────────────────────────────────────
public class RazorpayOrderDto { public string OrderId { get; set; } = string.Empty; public decimal Amount { get; set; } public string Currency { get; set; } = "INR"; public string Key { get; set; } = string.Empty; }
public class VerifyPaymentDto { public string RazorpayOrderId { get; set; } = string.Empty; public string RazorpayPaymentId { get; set; } = string.Empty; public string RazorpaySignature { get; set; } = string.Empty; public int OrderId { get; set; } }

// ── Dashboard ─────────────────────────────────────────────────────────────────
public class DashboardDto
{
    public decimal TodaySales { get; set; }
    public decimal MonthlySales { get; set; }
    public decimal YearlySales { get; set; }
    public int TodayOrders { get; set; }
    public int TotalProducts { get; set; }
    public int TotalCustomers { get; set; }
    public int LowStockCount { get; set; }
    public List<ChartDataDto> SalesChart { get; set; } = new();
    public List<TopProductDto> TopProducts { get; set; } = new();
    public List<RecentOrderDto> RecentOrders { get; set; } = new();
}
public class ChartDataDto { public string Label { get; set; } = string.Empty; public decimal Value { get; set; } }
public class TopProductDto { public string Name { get; set; } = string.Empty; public int Qty { get; set; } public decimal Revenue { get; set; } }
public class RecentOrderDto { public string Invoice { get; set; } = string.Empty; public string Customer { get; set; } = string.Empty; public decimal Amount { get; set; } public string Method { get; set; } = string.Empty; public DateTime Date { get; set; } }

// ── Discount ──────────────────────────────────────────────────────────────────
public class DiscountDto { public int Id { get; set; } public string Code { get; set; } = string.Empty; public string Type { get; set; } = string.Empty; public decimal Value { get; set; } public bool IsActive { get; set; } public int UsedCount { get; set; } }
public class CreateDiscountDto { public string Code { get; set; } = string.Empty; public string Type { get; set; } = "Percentage"; public decimal Value { get; set; } public decimal? MinOrderAmount { get; set; } public decimal? MaxDiscount { get; set; } public DateTime? ExpiryDate { get; set; } }
public class ValidateDiscountDto { public string Code { get; set; } = string.Empty; public decimal OrderAmount { get; set; } }
public class DiscountResultDto { public bool Valid { get; set; } public decimal DiscountAmount { get; set; } public string Message { get; set; } = string.Empty; }
