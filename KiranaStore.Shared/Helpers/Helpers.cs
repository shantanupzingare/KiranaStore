using System.Security.Cryptography;
using System.Text;

namespace KiranaStore.Shared.Helpers;

public static class PasswordHelper
{
    public static string Hash(string password)
    {
        using var sha = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password + "KiranaSalt2024!");
        return Convert.ToBase64String(sha.ComputeHash(bytes));
    }
    public static bool Verify(string password, string hash) => Hash(password) == hash;
}

public static class InvoiceHelper
{
    public static string Generate()
        => $"INV-{DateTime.UtcNow:yyyyMMdd}-{Random.Shared.Next(1000, 9999)}";
}
