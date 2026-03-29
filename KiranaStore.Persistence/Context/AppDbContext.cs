using Microsoft.EntityFrameworkCore;
using KiranaStore.Domain.Entities;
using KiranaStore.Shared.Helpers;

namespace KiranaStore.Persistence.Context;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<User>            Users            => Set<User>();
    public DbSet<Product>         Products         => Set<Product>();
    public DbSet<Category>        Categories       => Set<Category>();
    public DbSet<Customer>        Customers        => Set<Customer>();
    public DbSet<Supplier>        Suppliers        => Set<Supplier>();
    public DbSet<Order>           Orders           => Set<Order>();
    public DbSet<OrderItem>       OrderItems       => Set<OrderItem>();
    public DbSet<Payment>         Payments         => Set<Payment>();
    public DbSet<Discount>        Discounts        => Set<Discount>();
    public DbSet<NotificationLog> NotificationLogs => Set<NotificationLog>();
    public DbSet<StoreSettings>   StoreSettings    => Set<StoreSettings>();

    protected override void OnModelCreating(ModelBuilder m)
    {
        base.OnModelCreating(m);

        // Decimal precision
        foreach (var p in m.Model.GetEntityTypes().SelectMany(e => e.GetProperties())
            .Where(p => p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?)))
            p.SetColumnType("decimal(18,2)");

        // Relationships
        m.Entity<Order>().HasOne(o => o.Customer).WithMany(c => c.Orders).HasForeignKey(o => o.CustomerId).OnDelete(DeleteBehavior.SetNull);
        m.Entity<Order>().HasOne(o => o.User).WithMany(u => u.Orders).HasForeignKey(o => o.UserId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<OrderItem>().HasOne(i => i.Order).WithMany(o => o.Items).HasForeignKey(i => i.OrderId).OnDelete(DeleteBehavior.Cascade);
        m.Entity<OrderItem>().HasOne(i => i.Product).WithMany(p => p.OrderItems).HasForeignKey(i => i.ProductId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<Payment>().HasOne(p => p.Order).WithOne(o => o.Payment).HasForeignKey<Payment>(p => p.OrderId).OnDelete(DeleteBehavior.Cascade);
        m.Entity<Product>().HasOne(p => p.Category).WithMany(c => c.Products).HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Restrict);
        m.Entity<Product>().HasOne(p => p.Supplier).WithMany(s => s.Products).HasForeignKey(p => p.SupplierId).OnDelete(DeleteBehavior.SetNull);

        // Indexes
        m.Entity<User>().HasIndex(u => u.Email).IsUnique();
        m.Entity<Product>().HasIndex(p => p.SKU).IsUnique();
        m.Entity<Discount>().HasIndex(d => d.Code).IsUnique();
        m.Entity<Order>().HasIndex(o => o.InvoiceNumber).IsUnique();

        var seed = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        // Seed roles
        m.Entity<User>().HasData(new User
        {
            Id = 1, FullName = "Admin User", Email = "admin@kirana.com",
            PasswordHash = PasswordHelper.Hash("Admin@123"), Phone = "9999999999",
            Role = "Admin", IsActive = true, CreatedAt = seed
        });

        m.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Groceries",   CreatedAt = seed },
            new Category { Id = 2, Name = "Beverages",   CreatedAt = seed },
            new Category { Id = 3, Name = "Snacks",      CreatedAt = seed },
            new Category { Id = 4, Name = "Dairy",       CreatedAt = seed },
            new Category { Id = 5, Name = "Personal Care", CreatedAt = seed }
        );

        m.Entity<StoreSettings>().HasData(new StoreSettings
        {
            Id = 1, StoreName = "My Kirana Store",
            Address = "123 Main Street, City - 400001",
            Phone = "9876543210", Email = "store@kirana.com",
            GSTIN = "22AAAAA0000A1Z5", Currency = "INR", CreatedAt = seed
        });
    }
}
