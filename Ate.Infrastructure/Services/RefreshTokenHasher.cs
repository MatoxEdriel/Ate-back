using System.Security.Cryptography;
using System.Text;

namespace Ate.Infrastructure.Services;

/// <summary>En la base solo se guarda el SHA-256 del refresh token: una filtración no entrega sesiones válidas.</summary>
internal static class RefreshTokenHasher
{
    public static string Hash(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
