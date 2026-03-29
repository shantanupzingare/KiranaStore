using KiranaStore.Domain.Entities;
using KiranaStore.Domain.Interfaces;
using KiranaStore.Persistence.Context;

namespace KiranaStore.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _db;
    public IRepository<User>            Users            { get; }
    public IRepository<Product>         Products         { get; }
    public IRepository<Category>        Categories       { get; }
    public IRepository<Customer>        Customers        { get; }
    public IRepository<Supplier>        Suppliers        { get; }
    public IRepository<Order>           Orders           { get; }
    public IRepository<OrderItem>       OrderItems       { get; }
    public IRepository<Payment>         Payments         { get; }
    public IRepository<Discount>        Discounts        { get; }
    public IRepository<NotificationLog> NotificationLogs { get; }
    public IRepository<StoreSettings>   StoreSettings    { get; }

    public UnitOfWork(AppDbContext db)
    {
        _db              = db;
        Users            = new GenericRepository<User>(db);
        Products         = new GenericRepository<Product>(db);
        Categories       = new GenericRepository<Category>(db);
        Customers        = new GenericRepository<Customer>(db);
        Suppliers        = new GenericRepository<Supplier>(db);
        Orders           = new GenericRepository<Order>(db);
        OrderItems       = new GenericRepository<OrderItem>(db);
        Payments         = new GenericRepository<Payment>(db);
        Discounts        = new GenericRepository<Discount>(db);
        NotificationLogs = new GenericRepository<NotificationLog>(db);
        StoreSettings    = new GenericRepository<StoreSettings>(db);
    }

    public async Task<int> SaveChangesAsync() => await _db.SaveChangesAsync();
    public void Dispose() => _db.Dispose();
}
