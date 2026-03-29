namespace KiranaStore.Domain.Entities;
public class Customer : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? WhatsAppNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? GSTIN { get; set; }
    public decimal LoyaltyPoints { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
