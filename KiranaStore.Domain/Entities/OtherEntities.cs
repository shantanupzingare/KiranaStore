namespace KiranaStore.Domain.Entities;

public class OrderItem : BaseEntity
{
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal GSTRate { get; set; }
    public decimal GSTAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal Total { get; set; }
    public Order Order { get; set; } = null!;
    public Product Product { get; set; } = null!;
}

public class Payment : BaseEntity
{
    public int OrderId { get; set; }
    public decimal Amount { get; set; }
    public string Method { get; set; } = "Cash";
    public string Status { get; set; } = "Success";
    public string? TransactionId { get; set; }
    public string? RazorpayOrderId { get; set; }
    public string? RazorpayPaymentId { get; set; }
    public string? RazorpaySignature { get; set; }
    public DateTime PaidAt { get; set; } = DateTime.UtcNow;
    public Order Order { get; set; } = null!;
}

public class Discount : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public string Type { get; set; } = "Percentage";
    public decimal Value { get; set; }
    public decimal? MinOrderAmount { get; set; }
    public decimal? MaxDiscount { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsActive { get; set; } = true;
    public int UsageLimit { get; set; } = 100;
    public int UsedCount { get; set; }
}

public class NotificationLog : BaseEntity
{
    public int OrderId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Recipient { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
}

public class StoreSettings : BaseEntity
{
    public string StoreName { get; set; } = "Kirana Store";
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? GSTIN { get; set; }
    public string? LogoUrl { get; set; }
    public string Currency { get; set; } = "INR";
}
