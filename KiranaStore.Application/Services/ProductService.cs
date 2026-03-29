using Microsoft.EntityFrameworkCore;
using KiranaStore.Application.DTOs;
using KiranaStore.Application.Interfaces;
using KiranaStore.Application.Mapping;
using KiranaStore.Domain.Entities;
using KiranaStore.Domain.Interfaces;
using KiranaStore.Persistence.Context;
using KiranaStore.Shared.Responses;

namespace KiranaStore.Application.Services;

public class ProductService : IProductService
{
    private readonly IUnitOfWork _uow;
    private readonly AppDbContext _db;
    private readonly ICacheService _cache;

    public ProductService(IUnitOfWork uow, AppDbContext db, ICacheService cache)
    { _uow = uow; _db = db; _cache = cache; }

    public async Task<ApiResponse<PagedResponse<ProductDto>>> GetAllAsync(int page, int size, string? search, int? categoryId)
    {
        var q = _db.Products.Include(p => p.Category).Include(p => p.Supplier).Where(p => !p.IsDeleted);
        if (!string.IsNullOrEmpty(search))
            q = q.Where(p => p.Name.Contains(search) || p.SKU.Contains(search) || (p.Barcode != null && p.Barcode.Contains(search)));
        if (categoryId.HasValue) q = q.Where(p => p.CategoryId == categoryId);
        var total = await q.CountAsync();
        var items = await q.OrderBy(p => p.Name).Skip((page - 1) * size).Take(size).ToListAsync();
        return ApiResponse<PagedResponse<ProductDto>>.Ok(new PagedResponse<ProductDto>
        { Data = items.Select(p => p.ToDto()), TotalCount = total, Page = page, PageSize = size });
    }

    public async Task<ApiResponse<ProductDto>> GetByIdAsync(int id)
    {
        var p = await _db.Products.Include(p => p.Category).Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (p == null) return ApiResponse<ProductDto>.Fail("Product not found");
        return ApiResponse<ProductDto>.Ok(p.ToDto());
    }

    public async Task<ApiResponse<ProductDto>> CreateAsync(CreateProductDto dto)
    {
        var p = new Product { Name = dto.Name, SKU = dto.SKU, Barcode = dto.Barcode,
            CategoryId = dto.CategoryId, PurchasePrice = dto.PurchasePrice, SellingPrice = dto.SellingPrice,
            MRP = dto.MRP, Stock = dto.Stock, MinStockLevel = dto.MinStockLevel,
            Unit = dto.Unit, GSTRate = dto.GSTRate, SupplierId = dto.SupplierId };
        await _uow.Products.AddAsync(p); await _uow.SaveChangesAsync();
        await _cache.RemoveAsync("products_all");
        return await GetByIdAsync(p.Id);
    }

    public async Task<ApiResponse<ProductDto>> UpdateAsync(int id, UpdateProductDto dto)
    {
        var p = await _uow.Products.GetByIdAsync(id);
        if (p == null) return ApiResponse<ProductDto>.Fail("Not found");
        p.Name = dto.Name; p.SKU = dto.SKU; p.Barcode = dto.Barcode;
        p.CategoryId = dto.CategoryId; p.PurchasePrice = dto.PurchasePrice;
        p.SellingPrice = dto.SellingPrice; p.MRP = dto.MRP; p.Stock = dto.Stock;
        p.MinStockLevel = dto.MinStockLevel; p.Unit = dto.Unit; p.GSTRate = dto.GSTRate;
        p.SupplierId = dto.SupplierId; p.IsActive = dto.IsActive; p.UpdatedAt = DateTime.UtcNow;
        await _uow.Products.UpdateAsync(p); await _uow.SaveChangesAsync();
        await _cache.RemoveAsync("products_all");
        return await GetByIdAsync(id);
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id)
    {
        var p = await _uow.Products.GetByIdAsync(id);
        if (p == null) return ApiResponse<bool>.Fail("Not found");
        p.IsDeleted = true; await _uow.Products.UpdateAsync(p); await _uow.SaveChangesAsync();
        await _cache.RemoveAsync("products_all");
        return ApiResponse<bool>.Ok(true);
    }

    public async Task<ApiResponse<IEnumerable<ProductDto>>> GetLowStockAsync()
    {
        var items = await _db.Products.Include(p => p.Category)
            .Where(p => !p.IsDeleted && p.Stock <= p.MinStockLevel).ToListAsync();
        return ApiResponse<IEnumerable<ProductDto>>.Ok(items.Select(p => p.ToDto()));
    }

    public async Task<ApiResponse<IEnumerable<CategoryDto>>> GetCategoriesAsync()
    {
        var cats = await _db.Categories.Include(c => c.Products).Where(c => !c.IsDeleted).ToListAsync();
        return ApiResponse<IEnumerable<CategoryDto>>.Ok(cats.Select(c => c.ToDto()));
    }

    public async Task<ApiResponse<CategoryDto>> CreateCategoryAsync(CreateCategoryDto dto)
    {
        var c = new Category { Name = dto.Name, Description = dto.Description };
        await _uow.Categories.AddAsync(c); await _uow.SaveChangesAsync();
        return ApiResponse<CategoryDto>.Ok(c.ToDto());
    }
}
