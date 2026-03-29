using Microsoft.EntityFrameworkCore;
using KiranaStore.Application.DTOs;
using KiranaStore.Application.Interfaces;
using KiranaStore.Application.Mapping;
using KiranaStore.Domain.Interfaces;
using KiranaStore.Persistence.Context;
using KiranaStore.Shared.Responses;

using DomainCustomer  = KiranaStore.Domain.Entities.Customer;
using DomainSupplier  = KiranaStore.Domain.Entities.Supplier;
using DomainDiscount  = KiranaStore.Domain.Entities.Discount;

namespace KiranaStore.Application.Services;

// ── CustomerService ───────────────────────────────────────────────────────────
public class CustomerService : ICustomerService
{
    private readonly IUnitOfWork _uow;
    private readonly AppDbContext _db;
    public CustomerService(IUnitOfWork uow, AppDbContext db) { _uow = uow; _db = db; }

    public async Task<ApiResponse<PagedResponse<CustomerDto>>> GetAllAsync(int page, int size, string? search)
    {
        var q = _db.Customers.Include(c => c.Orders).Where(c => !c.IsDeleted);
        if (!string.IsNullOrEmpty(search))
            q = q.Where(c => c.Name.Contains(search) || c.Phone.Contains(search));
        var total = await q.CountAsync();
        var items = await q.OrderBy(c => c.Name).Skip((page - 1) * size).Take(size).ToListAsync();
        return ApiResponse<PagedResponse<CustomerDto>>.Ok(new PagedResponse<CustomerDto>
        { Data = items.Select(c => c.ToDto()), TotalCount = total, Page = page, PageSize = size });
    }

    public async Task<ApiResponse<CustomerDto>> GetByIdAsync(int id)
    {
        var c = await _db.Customers.Include(c => c.Orders)
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        if (c == null) return ApiResponse<CustomerDto>.Fail("Customer not found");
        return ApiResponse<CustomerDto>.Ok(c.ToDto());
    }

    public async Task<ApiResponse<CustomerDto>> CreateAsync(CreateCustomerDto dto)
    {
        var c = new DomainCustomer
        {
            Name           = dto.Name,
            Phone          = dto.Phone,
            WhatsAppNumber = dto.WhatsAppNumber,
            Email          = dto.Email,
            Address        = dto.Address,
            GSTIN          = dto.GSTIN
        };
        await _uow.Customers.AddAsync(c);
        await _uow.SaveChangesAsync();
        return ApiResponse<CustomerDto>.Ok(c.ToDto());
    }

    public async Task<ApiResponse<CustomerDto>> UpdateAsync(int id, CreateCustomerDto dto)
    {
        var c = await _uow.Customers.GetByIdAsync(id);
        if (c == null) return ApiResponse<CustomerDto>.Fail("Not found");
        c.Name = dto.Name; c.Phone = dto.Phone; c.WhatsAppNumber = dto.WhatsAppNumber;
        c.Email = dto.Email; c.Address = dto.Address; c.GSTIN = dto.GSTIN;
        c.UpdatedAt = DateTime.UtcNow;
        await _uow.Customers.UpdateAsync(c);
        await _uow.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id)
    {
        var c = await _uow.Customers.GetByIdAsync(id);
        if (c == null) return ApiResponse<bool>.Fail("Not found");
        c.IsDeleted = true;
        await _uow.Customers.UpdateAsync(c);
        await _uow.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true);
    }
}

// ── SupplierService ───────────────────────────────────────────────────────────
public class SupplierService : ISupplierService
{
    private readonly IUnitOfWork _uow;
    private readonly AppDbContext _db;
    public SupplierService(IUnitOfWork uow, AppDbContext db) { _uow = uow; _db = db; }

    public async Task<ApiResponse<PagedResponse<SupplierDto>>> GetAllAsync(int page, int size, string? search)
    {
        var q = _db.Suppliers.Include(s => s.Products).Where(s => !s.IsDeleted);
        if (!string.IsNullOrEmpty(search)) q = q.Where(s => s.Name.Contains(search));
        var total = await q.CountAsync();
        var items = await q.Skip((page - 1) * size).Take(size).ToListAsync();
        return ApiResponse<PagedResponse<SupplierDto>>.Ok(new PagedResponse<SupplierDto>
        { Data = items.Select(s => s.ToDto()), TotalCount = total, Page = page, PageSize = size });
    }

    public async Task<ApiResponse<SupplierDto>> GetByIdAsync(int id)
    {
        var s = await _db.Suppliers.Include(s => s.Products)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);
        if (s == null) return ApiResponse<SupplierDto>.Fail("Not found");
        return ApiResponse<SupplierDto>.Ok(s.ToDto());
    }

    public async Task<ApiResponse<SupplierDto>> CreateAsync(CreateSupplierDto dto)
    {
        var s = new DomainSupplier
        { Name = dto.Name, Phone = dto.Phone, Email = dto.Email, Address = dto.Address, GSTIN = dto.GSTIN };
        await _uow.Suppliers.AddAsync(s);
        await _uow.SaveChangesAsync();
        return ApiResponse<SupplierDto>.Ok(s.ToDto());
    }

    public async Task<ApiResponse<SupplierDto>> UpdateAsync(int id, CreateSupplierDto dto)
    {
        var s = await _uow.Suppliers.GetByIdAsync(id);
        if (s == null) return ApiResponse<SupplierDto>.Fail("Not found");
        s.Name = dto.Name; s.Phone = dto.Phone; s.Email = dto.Email;
        s.Address = dto.Address; s.GSTIN = dto.GSTIN; s.UpdatedAt = DateTime.UtcNow;
        await _uow.Suppliers.UpdateAsync(s);
        await _uow.SaveChangesAsync();
        return await GetByIdAsync(id);
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id)
    {
        var s = await _uow.Suppliers.GetByIdAsync(id);
        if (s == null) return ApiResponse<bool>.Fail("Not found");
        s.IsDeleted = true;
        await _uow.Suppliers.UpdateAsync(s);
        await _uow.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true);
    }
}

// ── DashboardService ──────────────────────────────────────────────────────────
public class DashboardService : IDashboardService
{
    private readonly AppDbContext _db;
    private readonly ICacheService _cache;
    public DashboardService(AppDbContext db, ICacheService cache) { _db = db; _cache = cache; }

    public async Task<ApiResponse<DashboardDto>> GetStatsAsync()
    {
        var cacheKey = "dashboard_stats";
        var cached = await _cache.GetAsync<DashboardDto>(cacheKey);
        if (cached != null) return ApiResponse<DashboardDto>.Ok(cached);

        var today      = DateTime.UtcNow.Date;
        var monthStart = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var yearStart  = new DateTime(today.Year, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        var dto = new DashboardDto
        {
            TodaySales     = await _db.Orders.Where(o => !o.IsDeleted && o.OrderDate >= today).SumAsync(o => (decimal?)o.TotalAmount) ?? 0,
            MonthlySales   = await _db.Orders.Where(o => !o.IsDeleted && o.OrderDate >= monthStart).SumAsync(o => (decimal?)o.TotalAmount) ?? 0,
            YearlySales    = await _db.Orders.Where(o => !o.IsDeleted && o.OrderDate >= yearStart).SumAsync(o => (decimal?)o.TotalAmount) ?? 0,
            TodayOrders    = await _db.Orders.CountAsync(o => !o.IsDeleted && o.OrderDate >= today),
            TotalProducts  = await _db.Products.CountAsync(p => !p.IsDeleted && p.IsActive),
            TotalCustomers = await _db.Customers.CountAsync(c => !c.IsDeleted),
            LowStockCount  = await _db.Products.CountAsync(p => !p.IsDeleted && p.Stock <= p.MinStockLevel)
        };

        dto.SalesChart = await _db.Orders
            .Where(o => !o.IsDeleted && o.OrderDate >= DateTime.UtcNow.AddDays(-7))
            .GroupBy(o => o.OrderDate.Date)
            .Select(g => new ChartDataDto { Label = g.Key.ToString("dd MMM"), Value = g.Sum(o => o.TotalAmount) })
            .OrderBy(x => x.Label).ToListAsync();

        dto.TopProducts = await _db.OrderItems
            .Where(i => !i.IsDeleted)
            .GroupBy(i => new { i.ProductId, i.ProductName })
            .Select(g => new TopProductDto { Name = g.Key.ProductName, Qty = g.Sum(i => i.Quantity), Revenue = g.Sum(i => i.Total) })
            .OrderByDescending(x => x.Qty).Take(5).ToListAsync();

        dto.RecentOrders = await _db.Orders
            .Include(o => o.Customer)
            .Where(o => !o.IsDeleted)
            .OrderByDescending(o => o.OrderDate).Take(5)
            .Select(o => new RecentOrderDto
            {
                Invoice  = o.InvoiceNumber,
                Customer = o.Customer != null ? o.Customer.Name : "Walk-in",  
                Amount   = o.TotalAmount,
                Method   = o.PaymentMethod,
                Date     = o.OrderDate
            }).ToListAsync();

        await _cache.SetAsync("dashboard_stats", dto, TimeSpan.FromMinutes(5));
        return ApiResponse<DashboardDto>.Ok(dto);
    }

    public async Task<ApiResponse<IEnumerable<ChartDataDto>>> GetSalesChartAsync(string period)
    {
        var start = period switch
        {
            "month" => DateTime.UtcNow.AddDays(-30),
            "year"  => DateTime.UtcNow.AddDays(-365),
            _       => DateTime.UtcNow.AddDays(-7)
        };

        var data = await _db.Orders
            .Where(o => !o.IsDeleted && o.OrderDate >= start)
            .GroupBy(o => o.OrderDate.Date)
            .Select(g => new ChartDataDto { Label = g.Key.ToString("dd MMM"), Value = g.Sum(o => o.TotalAmount) })
            .OrderBy(x => x.Label).ToListAsync();

        return ApiResponse<IEnumerable<ChartDataDto>>.Ok(data);
    }
}

// ── DiscountService ───────────────────────────────────────────────────────────
public class DiscountService : IDiscountService
{
    private readonly IUnitOfWork _uow;
    private readonly AppDbContext _db;
    public DiscountService(IUnitOfWork uow, AppDbContext db) { _uow = uow; _db = db; }

    public async Task<ApiResponse<DiscountResultDto>> ValidateAsync(ValidateDiscountDto dto)
    {
        var disc = await _db.Discounts.FirstOrDefaultAsync(d =>
            d.Code == dto.Code && d.IsActive && !d.IsDeleted &&
            (d.ExpiryDate == null || d.ExpiryDate > DateTime.UtcNow) && d.UsedCount < d.UsageLimit);

        if (disc == null)
            return ApiResponse<DiscountResultDto>.Ok(
                new DiscountResultDto { Valid = false, Message = "Invalid or expired code" });

        if (disc.MinOrderAmount.HasValue && dto.OrderAmount < disc.MinOrderAmount)
            return ApiResponse<DiscountResultDto>.Ok(
                new DiscountResultDto { Valid = false, Message = $"Minimum order ₹{disc.MinOrderAmount} required" });

        var amount = disc.Type == "Percentage"
            ? Math.Min(dto.OrderAmount * disc.Value / 100m, disc.MaxDiscount ?? decimal.MaxValue)
            : disc.Value;

        return ApiResponse<DiscountResultDto>.Ok(new DiscountResultDto
        { Valid = true, DiscountAmount = amount, Message = $"Code applied! Save ₹{amount:F2}" });
    }

    public async Task<ApiResponse<IEnumerable<DiscountDto>>> GetAllAsync()
    {
        var items = await _db.Discounts.Where(d => !d.IsDeleted).ToListAsync();
        return ApiResponse<IEnumerable<DiscountDto>>.Ok(items.Select(d => d.ToDto()));
    }

    public async Task<ApiResponse<DiscountDto>> CreateAsync(CreateDiscountDto dto)
    {
        var d = new DomainDiscount
        {
            Code           = dto.Code,
            Type           = dto.Type,
            Value          = dto.Value,
            MinOrderAmount = dto.MinOrderAmount,
            MaxDiscount    = dto.MaxDiscount,
            ExpiryDate     = dto.ExpiryDate
        };
        await _uow.Discounts.AddAsync(d);
        await _uow.SaveChangesAsync();
        return ApiResponse<DiscountDto>.Ok(d.ToDto());
    }

    public async Task<ApiResponse<bool>> DeleteAsync(int id)
    {
        var d = await _uow.Discounts.GetByIdAsync(id);
        if (d == null) return ApiResponse<bool>.Fail("Not found");
        d.IsDeleted = true;
        await _uow.Discounts.UpdateAsync(d);
        await _uow.SaveChangesAsync();
        return ApiResponse<bool>.Ok(true);
    }
}
