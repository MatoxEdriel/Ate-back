using System.Globalization;
using System.Security.Cryptography;
using Ate.Application.Common.Interfaces;

namespace Ate.Infrastructure.Services;

/// <summary>
/// PBKDF2-HMAC-SHA256 con sal aleatoria por contraseña.
/// Formato actual: <c>v2$&lt;iteraciones&gt;$&lt;base64(sal + hash)&gt;</c>. El formato antiguo (solo base64,
/// 100.000 iteraciones) se sigue aceptando y se actualiza en el siguiente login (<see cref="NeedsRehash"/>).
/// </summary>
public class PasswordHasher : IPasswordHasher, ISingletonDependency
{
    private const int SaltSize = 16;
    private const int HashSize = 32;

    /// <summary>
    /// OWASP Password Storage Cheat Sheet: para PBKDF2-HMAC-SHA256 recomienda como mínimo 600.000 iteraciones
    /// (210.000 para SHA-512 y 1.300.000 para SHA-1). Es el número de veces que se repite el cálculo: vuelve
    /// cada intento de adivinar una contraseña robada lo bastante caro.
    /// https://cheatsheetseries.owasp.org/cheatsheets/Password_Storage_Cheat_Sheet.html#pbkdf2
    /// </summary>
    private const int Iterations = 600_000;

    /// <summary>Iteraciones del formato antiguo (sin prefijo), solo para verificar hashes ya guardados.</summary>
    private const int LegacyIterations = 100_000;

    private const string Version = "v2";

    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, HashSize);
        return $"{Version}${Iterations.ToString(CultureInfo.InvariantCulture)}${Convert.ToBase64String([.. salt, .. hash])}";
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        if (!TryParse(passwordHash, out var iterations, out var combined) || combined.Length != SaltSize + HashSize)
        {
            return false;
        }

        var salt = combined.AsSpan(0, SaltSize);
        var expected = combined.AsSpan(SaltSize, HashSize);
        var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, Algorithm, HashSize);
        return CryptographicOperations.FixedTimeEquals(expected, actual);
    }

    public bool NeedsRehash(string passwordHash) =>
        !TryParse(passwordHash, out var iterations, out _) || iterations < Iterations || !passwordHash.StartsWith($"{Version}$", StringComparison.Ordinal);

    private static bool TryParse(string passwordHash, out int iterations, out byte[] combined)
    {
        iterations = LegacyIterations;
        combined = [];
        try
        {
            var parts = passwordHash.Split('$');
            if (parts.Length == 3 && parts[0] == Version)
            {
                if (!int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out iterations) || iterations <= 0)
                {
                    return false;
                }

                combined = Convert.FromBase64String(parts[2]);
                return true;
            }

            combined = Convert.FromBase64String(passwordHash);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
