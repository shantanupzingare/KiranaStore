using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using KiranaStore.Application.DTOs;
using KiranaStore.Application.Interfaces;

namespace KiranaStore.API.Controllers;

[ApiController] [Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _svc;
    public AuthController(IAuthService svc) => _svc = svc;

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest dto)
        => (await _svc.LoginAsync(dto)).Success ? Ok(await _svc.LoginAsync(dto)) : Unauthorized(await _svc.LoginAsync(dto));

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshRequest dto)
    { var r = await _svc.RefreshAsync(dto); return r.Success ? Ok(r) : Unauthorized(r); }

    [HttpPost("register"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Register([FromBody] CreateUserDto dto)
    { var r = await _svc.RegisterAsync(dto); return r.Success ? Ok(r) : BadRequest(r); }
}

[ApiController] [Route("api/[controller]"), Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductService _svc;
    public ProductsController(IProductService svc) => _svc = svc;

    [HttpGet] public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int size = 20, [FromQuery] string? search = null, [FromQuery] int? categoryId = null)
        => Ok(await _svc.GetAllAsync(page, size, search, categoryId));

    [HttpGet("{id}")] public async Task<IActionResult> GetById(int id)
    { var r = await _svc.GetByIdAsync(id); return r.Success ? Ok(r) : NotFound(r); }

    [HttpPost, Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
    { var r = await _svc.CreateAsync(dto); return r.Success ? Ok(r) : BadRequest(r); }

    [HttpPut("{id}"), Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductDto dto)
    { var r = await _svc.UpdateAsync(id, dto); return r.Success ? Ok(r) : BadRequest(r); }

    [HttpDelete("{id}"), Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    { var r = await _svc.DeleteAsync(id); return r.Success ? Ok(r) : NotFound(r); }

    [HttpGet("low-stock")] public async Task<IActionResult> LowStock() => Ok(await _svc.GetLowStockAsync());
    [HttpGet("categories")] public async Task<IActionResult> Categories() => Ok(await _svc.GetCategoriesAsync());
    [HttpPost("categories"), Authorize(Roles = "Admin,Manager")]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto dto)
    { var r = await _svc.CreateCategoryAsync(dto); return Ok(r); }
}

[ApiController] [Route("api/[controller]"), Authorize]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _svc;
    public CustomersController(ICustomerService svc) => _svc = svc;

    [HttpGet] public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int size = 20, [FromQuery] string? search = null)
        => Ok(await _svc.GetAllAsync(page, size, search));
    [HttpGet("{id}")] public async Task<IActionResult> GetById(int id) { var r = await _svc.GetByIdAsync(id); return r.Success ? Ok(r) : NotFound(r); }
    [HttpPost] public async Task<IActionResult> Create([FromBody] CreateCustomerDto dto) { var r = await _svc.CreateAsync(dto); return Ok(r); }
    [HttpPut("{id}")] public async Task<IActionResult> Update(int id, [FromBody] CreateCustomerDto dto) { var r = await _svc.UpdateAsync(id, dto); return Ok(r); }
    [HttpDelete("{id}"), Authorize(Roles = "Admin")] public async Task<IActionResult> Delete(int id) { var r = await _svc.DeleteAsync(id); return r.Success ? Ok(r) : NotFound(r); }
}

[ApiController] [Route("api/[controller]"), Authorize]
public class SuppliersController : ControllerBase
{
    private readonly ISupplierService _svc;
    public SuppliersController(ISupplierService svc) => _svc = svc;

    [HttpGet] public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int size = 20, [FromQuery] string? search = null)
        => Ok(await _svc.GetAllAsync(page, size, search));
    [HttpGet("{id}")] public async Task<IActionResult> GetById(int id) { var r = await _svc.GetByIdAsync(id); return r.Success ? Ok(r) : NotFound(r); }
    [HttpPost, Authorize(Roles = "Admin,Manager")] public async Task<IActionResult> Create([FromBody] CreateSupplierDto dto) => Ok(await _svc.CreateAsync(dto));
    [HttpPut("{id}"), Authorize(Roles = "Admin,Manager")] public async Task<IActionResult> Update(int id, [FromBody] CreateSupplierDto dto) => Ok(await _svc.UpdateAsync(id, dto));
    [HttpDelete("{id}"), Authorize(Roles = "Admin")] public async Task<IActionResult> Delete(int id) { var r = await _svc.DeleteAsync(id); return r.Success ? Ok(r) : NotFound(r); }
}

[ApiController] [Route("api/[controller]"), Authorize]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _svc;
    public OrdersController(IOrderService svc) => _svc = svc;

    [HttpGet] public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int size = 20, [FromQuery] DateTime? from = null, [FromQuery] DateTime? to = null)
        => Ok(await _svc.GetAllAsync(page, size, from, to));
    [HttpGet("{id}")] public async Task<IActionResult> GetById(int id) { var r = await _svc.GetByIdAsync(id); return r.Success ? Ok(r) : NotFound(r); }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderDto dto)
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "1");
        var r = await _svc.CreateAsync(dto, userId);
        return r.Success ? Ok(r) : BadRequest(r);
    }

    [HttpDelete("{id}"), Authorize(Roles = "Admin,Manager")] public async Task<IActionResult> Cancel(int id) { var r = await _svc.CancelAsync(id); return Ok(r); }

    [HttpGet("{id}/invoice")]
    public async Task<IActionResult> Invoice(int id)
    {
        var r = await _svc.GenerateInvoicePdfAsync(id);
        if (!r.Success) return NotFound(r);
        return File(r.Data!, "application/pdf", $"invoice-{id}.pdf");
    }
}

[ApiController] [Route("api/[controller]"), Authorize]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentService _svc;
    public PaymentsController(IPaymentService svc) => _svc = svc;

    [HttpPost("create-order/{orderId}")] public async Task<IActionResult> CreateOrder(int orderId) => Ok(await _svc.CreateRazorpayOrderAsync(orderId));
    [HttpPost("verify")] public async Task<IActionResult> Verify([FromBody] VerifyPaymentDto dto) { var r = await _svc.VerifyPaymentAsync(dto); return r.Success ? Ok(r) : BadRequest(r); }
}

[ApiController] [Route("api/[controller]"), Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _svc;
    public DashboardController(IDashboardService svc) => _svc = svc;

    [HttpGet] public async Task<IActionResult> Get() => Ok(await _svc.GetStatsAsync());
    [HttpGet("chart")] public async Task<IActionResult> Chart([FromQuery] string period = "week") => Ok(await _svc.GetSalesChartAsync(period));
}

[ApiController] [Route("api/[controller]"), Authorize]
public class DiscountsController : ControllerBase
{
    private readonly IDiscountService _svc;
    public DiscountsController(IDiscountService svc) => _svc = svc;

    [HttpGet] public async Task<IActionResult> GetAll() => Ok(await _svc.GetAllAsync());
    [HttpPost("validate")] public async Task<IActionResult> Validate([FromBody] ValidateDiscountDto dto) => Ok(await _svc.ValidateAsync(dto));
    [HttpPost, Authorize(Roles = "Admin,Manager")] public async Task<IActionResult> Create([FromBody] CreateDiscountDto dto) => Ok(await _svc.CreateAsync(dto));
    [HttpDelete("{id}"), Authorize(Roles = "Admin")] public async Task<IActionResult> Delete(int id) => Ok(await _svc.DeleteAsync(id));
}
