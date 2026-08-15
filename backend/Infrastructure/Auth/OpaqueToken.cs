using System.Security.Cryptography;
using System.Text;

namespace Infrastructure.Auth;

public static class OpaqueToken
{
    public static string Generate()
    {
        return Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }

    public static string Hash(string token)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    }
}
