using KiranaStore.Application.DTOs;
using KiranaStore.Shared.Responses;

namespace KiranaStore.Application.Interfaces;

public interface IAuthService
{
    Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest req);
    Task<ApiResponse<LoginResponse>> RefreshAsync(RefreshRequest req);
    Task<ApiResponse<UserDto>> RegisterAsync(CreateUserDto dto);
}

public interface IProductService
{
    Task<ApiResponse<PagedResponse<ProductDto>>> GetAllAsync(int page, int size, string? search, int? categoryId);
    Task<ApiResponse<ProductDto>> GetByIdAsync(int id);
    Task<ApiResponse<ProductDto>> CreateAsync(CreateProductDto dto);
    Task<ApiResponse<ProductDto>> UpdateAsync(int id, UpdateProductDto dto);
    Task<ApiResponse<bool>> DeleteAsync(int id);
    Task<ApiResponse<IEnumerable<ProductDto>>> GetLowStockAsync();
    Task<ApiResponse<IEnumerable<CategoryDto>>> GetCategoriesAsync();
    Task<ApiResponse<CategoryDto>> CreateCategoryAsync(CreateCategoryDto dto);
}

public interface ICustomerService
{
    Task<ApiResponse<PagedResponse<CustomerDto>>> GetAllAsync(int page, int size, string? search);
    Task<ApiResponse<CustomerDto>> GetByIdAsync(int id);
    Task<ApiResponse<CustomerDto>> CreateAsync(CreateCustomerDto dto);
    Task<ApiResponse<CustomerDto>> UpdateAsync(int id, CreateCustomerDto dto);
    Task<ApiResponse<bool>> DeleteAsync(int id);
}

public interface ISupplierService
{
    Task<ApiResponse<PagedResponse<SupplierDto>>> GetAllAsync(int page, int size, string? search);
    Task<ApiResponse<SupplierDto>> GetByIdAsync(int id);
    Task<ApiResponse<SupplierDto>> CreateAsync(CreateSupplierDto dto);
    Task<ApiResponse<SupplierDto>> UpdateAsync(int id, CreateSupplierDto dto);
    Task<ApiResponse<bool>> DeleteAsync(int id);
}

public interface IOrderService
{
    Task<ApiResponse<PagedResponse<OrderDto>>> GetAllAsync(int page, int size, DateTime? from, DateTime? to);
    Task<ApiResponse<OrderDto>> GetByIdAsync(int id);
    Task<ApiResponse<OrderDto>> CreateAsync(CreateOrderDto dto, int userId);
    Task<ApiResponse<bool>> CancelAsync(int id);
    Task<ApiResponse<byte[]>> GenerateInvoicePdfAsync(int id);
}

public interface IPaymentService
{
    Task<ApiResponse<RazorpayOrderDto>> CreateRazorpayOrderAsync(int orderId);
    Task<ApiResponse<bool>> VerifyPaymentAsync(VerifyPaymentDto dto);
}

public interface IDashboardService
{
    Task<ApiResponse<DashboardDto>> GetStatsAsync();
    Task<ApiResponse<IEnumerable<ChartDataDto>>> GetSalesChartAsync(string period);
}

public interface IDiscountService
{
    Task<ApiResponse<DiscountResultDto>> ValidateAsync(ValidateDiscountDto dto);
    Task<ApiResponse<IEnumerable<DiscountDto>>> GetAllAsync();
    Task<ApiResponse<DiscountDto>> CreateAsync(CreateDiscountDto dto);
    Task<ApiResponse<bool>> DeleteAsync(int id);
}

public interface IEmailService
{
    Task<bool> SendInvoiceAsync(string toEmail, string customerName, string invoiceNumber, byte[] pdfBytes);
}

public interface IWhatsAppService
{
    Task<bool> SendInvoiceAsync(string phone, string customerName, string invoiceNumber, string invoiceUrl);
}

public interface IInvoiceService
{
    Task<byte[]> GeneratePdfAsync(OrderDto order, string storeName, string storeAddress, string storeGstin);
}

public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    Task RemoveAsync(string key);
}
