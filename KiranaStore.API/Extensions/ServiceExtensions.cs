using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using KiranaStore.Application.Interfaces;
using KiranaStore.Application.Services;
using KiranaStore.Domain.Interfaces;
using KiranaStore.Infrastructure.Services;
using KiranaStore.Persistence.Context;
using KiranaStore.Persistence.Repositories;

namespace KiranaStore.API.Extensions;

public static class ServiceExtensions
{
    public static IServiceCollection AddApp(this IServiceCollection s, IConfiguration c)
    {
        // =========================
        // DATABASE CONFIGURATION
        // =========================
        s.AddDbContext<AppDbContext>(o => o.UseSqlServer(
            c.GetConnectionString("Default"),
            b => b.MigrationsAssembly("KiranaStore.Persistence")));

        // =========================
        // HTTP CLIENT
        // =========================
        s.AddHttpClient();

        // =========================
        // REDIS CACHE (FIXED ✅)
        // =========================
        var redisConn = c["Redis:Connection"] ?? "localhost:6379";

        s.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConn;
            options.InstanceName = "Kirana_"; // Prefix for all keys
        });

        // =========================
        // REPOSITORY + UNIT OF WORK
        // =========================
        s.AddScoped(typeof(IRepository<>), typeof(GenericRepository<>));
        s.AddScoped<IUnitOfWork, UnitOfWork>();

        // =========================
        // APPLICATION SERVICES
        // =========================
        s.AddScoped<IAuthService, AuthService>();
        s.AddScoped<IProductService, ProductService>();
        s.AddScoped<ICustomerService, CustomerService>();
        s.AddScoped<ISupplierService, SupplierService>();
        s.AddScoped<IOrderService, OrderService>();
        s.AddScoped<IPaymentService, PaymentService>();
        s.AddScoped<IDashboardService, DashboardService>();
        s.AddScoped<IDiscountService, DiscountService>();
        s.AddScoped<ICacheService, CacheService>();

        // =========================
        // INFRASTRUCTURE SERVICES
        // =========================
        s.AddScoped<IEmailService, EmailService>();
        s.AddScoped<IWhatsAppService, WhatsAppService>();
        s.AddScoped<IInvoiceService, InvoiceService>();

        return s;
    }

    public static IServiceCollection AddJwt(this IServiceCollection s, IConfiguration c)
    {
        var key = Encoding.UTF8.GetBytes(c["Jwt:Key"]!);

        s.AddAuthentication(o =>
        {
            o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(o =>
        {
            o.RequireHttpsMetadata = false;
            o.SaveToken = true;

            o.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = c["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = c["Jwt:Audience"],
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        });

        return s;
    }

    public static IServiceCollection AddSwagger(this IServiceCollection s)
    {
        s.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "KiranaStore API",
                Version = "v1"
            });

            // JWT Auth in Swagger
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "Enter JWT token like: Bearer {token}",
                Name = "Authorization",
                In = ParameterLocation.Header,
                Type = SecuritySchemeType.ApiKey,
                Scheme = "Bearer"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id   = "Bearer"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return s;
    }
}