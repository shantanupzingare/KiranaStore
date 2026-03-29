using KiranaStore.Domain.Entities;

namespace KiranaStore.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepository<User> Users { get; }
    IRepository<Product> Products { get; }
    IRepository<Category> Categories { get; }
    IRepository<Customer> Customers { get; }
    IRepository<Supplier> Suppliers { get; }
    IRepository<Order> Orders { get; }
    IRepository<OrderItem> OrderItems { get; }
    IRepository<Payment> Payments { get; }
    IRepository<Discount> Discounts { get; }
    IRepository<NotificationLog> NotificationLogs { get; }
    IRepository<StoreSettings> StoreSettings { get; }
    Task<int> SaveChangesAsync();
}
