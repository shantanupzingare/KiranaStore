using Microsoft.EntityFrameworkCore;
using KiranaStore.Domain.Interfaces;
using KiranaStore.Persistence.Context;

namespace KiranaStore.Persistence.Repositories;

public class GenericRepository<T> : IRepository<T> where T : class
{
    protected readonly AppDbContext _db;
    protected readonly DbSet<T> _set;
    public GenericRepository(AppDbContext db) { _db = db; _set = db.Set<T>(); }
    public async Task<T?> GetByIdAsync(int id) => await _set.FindAsync(id);
    public async Task<IEnumerable<T>> GetAllAsync() => await _set.ToListAsync();
    public async Task<T> AddAsync(T e) { await _set.AddAsync(e); return e; }
    public Task UpdateAsync(T e) { _set.Update(e); return Task.CompletedTask; }
    public async Task DeleteAsync(int id) { var e = await GetByIdAsync(id); if (e != null) _set.Remove(e); }
}
