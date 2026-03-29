namespace KiranaStore.Domain.Entities;
public class Order : BaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public int? CustomerId { get; set; }
    public int UserId { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal GSTAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public string PaymentMethod { get; set; } = "Cash";
    public string PaymentStatus { get; set; } = "Paid";
    public string OrderStatus { get; set; } = "Completed";
    public string? RazorpayOrderId { get; set; }
    public string? RazorpayPaymentId { get; set; }
    public string? Notes { get; set; }
    public bool EmailSent { get; set; }
    public bool WhatsAppSent { get; set; }
    public Customer? Customer { get; set; }
    public User User { get; set; } = null!;
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
    public Payment? Payment { get; set; }
}
