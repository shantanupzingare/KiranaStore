using System.Net;
using System.Text.Json;
using KiranaStore.Shared.Responses;

namespace KiranaStore.API.Middleware;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionMiddleware> _log;
    public ExceptionMiddleware(RequestDelegate next, ILogger<ExceptionMiddleware> log) { _next = next; _log = log; }

    public async Task InvokeAsync(HttpContext ctx)
    {
        try { await _next(ctx); }
        catch (Exception ex)
        {
            _log.LogError(ex, "Unhandled exception");
            ctx.Response.ContentType = "application/json";
            ctx.Response.StatusCode  = (int)HttpStatusCode.InternalServerError;
            await ctx.Response.WriteAsync(JsonSerializer.Serialize(
                ApiResponse<object>.Fail("An unexpected error occurred"),
                new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
        }
    }
}
