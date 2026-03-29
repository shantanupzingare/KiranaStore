using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using KiranaStore.Application.DTOs;
using KiranaStore.Application.Interfaces;
using KiranaStore.Application.Mapping;
using KiranaStore.Domain.Entities;
using KiranaStore.Domain.Interfaces;
using KiranaStore.Persistence.Context;
using KiranaStore.Shared.Helpers;
using KiranaStore.Shared.Responses;

namespace KiranaStore.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUnitOfWork _uow;
    private readonly IConfiguration _config;
    private readonly AppDbContext _db;

    public AuthService(IUnitOfWork uow, IConfiguration config, AppDbContext db)
    { _uow = uow; _config = config; _db = db; }

    public async Task<ApiResponse<LoginResponse>> LoginAsync(LoginRequest req)
    {
        var user = await _db.Users.Include(u => u.Orders)
            .FirstOrDefaultAsync(u => u.Email == req.Email && !u.IsDeleted && u.IsActive);

        if (user == null || !PasswordHelper.Verify(req.Password, user.PasswordHash))
            return ApiResponse<LoginResponse>.Fail("Invalid email or password");

        var token   = GenerateToken(user);
        var refresh = GenerateRefresh();
        user.RefreshToken       = refresh;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _uow.Users.UpdateAsync(user);
        await _uow.SaveChangesAsync();

        return ApiResponse<LoginResponse>.Ok(new LoginResponse(token, refresh,
            DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:ExpiryMinutes"] ?? "60")), user.ToDto()));
    }

    public async Task<ApiResponse<LoginResponse>> RefreshAsync(RefreshRequest req)
    {
        var principal = GetPrincipal(req.Token);
        if (principal == null) return ApiResponse<LoginResponse>.Fail("Invalid token");
        var id = int.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (user == null || user.RefreshToken != req.RefreshToken || user.RefreshTokenExpiry < DateTime.UtcNow)
            return ApiResponse<LoginResponse>.Fail("Invalid refresh token");

        var token = GenerateToken(user); var refresh = GenerateRefresh();
        user.RefreshToken = refresh; user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await _uow.Users.UpdateAsync(user); await _uow.SaveChangesAsync();
        return ApiResponse<LoginResponse>.Ok(new LoginResponse(token, refresh,
            DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:ExpiryMinutes"] ?? "60")), user.ToDto()));
    }

    public async Task<ApiResponse<UserDto>> RegisterAsync(CreateUserDto dto)
    {
        if (await _db.Users.AnyAsync(u => u.Email == dto.Email))
            return ApiResponse<UserDto>.Fail("Email already exists");
        var user = new User { FullName = dto.FullName, Email = dto.Email, Phone = dto.Phone,
            Role = dto.Role, PasswordHash = PasswordHelper.Hash(dto.Password) };
        await _uow.Users.AddAsync(user); await _uow.SaveChangesAsync();
        return ApiResponse<UserDto>.Ok(user.ToDto(), "Registered successfully");
    }

    private string GenerateToken(User user)
    {
        var key   = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[] {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Email,          user.Email),
            new Claim(ClaimTypes.Name,           user.FullName),
            new Claim(ClaimTypes.Role,           user.Role)
        };
        var token = new JwtSecurityToken(_config["Jwt:Issuer"], _config["Jwt:Audience"],
            claims, expires: DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:ExpiryMinutes"] ?? "60")),
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRefresh()
    { var b = new byte[64]; RandomNumberGenerator.Fill(b); return Convert.ToBase64String(b); }

    private ClaimsPrincipal? GetPrincipal(string token)
    {
        try {
            var p = new JwtSecurityTokenHandler().ValidateToken(token, new TokenValidationParameters {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]!)),
                ValidateIssuer = false, ValidateAudience = false, ValidateLifetime = false
            }, out var sec);
            return sec is JwtSecurityToken jwt &&
                jwt.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase)
                ? p : null;
        } catch { return null; }
    }
}
