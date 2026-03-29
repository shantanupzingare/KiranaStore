using KiranaStore.API.Extensions;
using KiranaStore.API.Middleware;
using KiranaStore.Infrastructure.Logging;
using KiranaStore.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);
SerilogConfig.Configure(builder.Configuration);
builder.Host.UseSerilog();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddApp(builder.Configuration);
builder.Services.AddJwt(builder.Configuration);
builder.Services.AddSwagger();
builder.Services.AddCors(o => o.AddPolicy("AllowAll", p =>
    p.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    try { scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate(); Log.Information("DB migrated"); }
    catch (Exception ex) { Log.Error(ex, "DB migration failed"); }
}

if (app.Environment.IsDevelopment())
{ app.UseSwagger(); app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "KiranaStore API"); c.RoutePrefix = string.Empty; }); }

app.UseSerilogRequestLogging();
app.UseMiddleware<ExceptionMiddleware>();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
Log.Information("KiranaStore API started");
app.Run();
