namespace KiranaStore.Domain.Entities;
public class Product : BaseEntity
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
    public bool IsActive { get; set; } = true;
    public string? ImageUrl { get; set; }
    public int? SupplierId { get; set; }
    public Category Category { get; set; } = null!;
    public Supplier? Supplier { get; set; }
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
}
