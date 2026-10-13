using System.Security.Cryptography;
using Ate.Application.Common.Interfaces;

namespace Ate.Infrastructure.Services;

public class PasswordHasher : IPasswordHasher, ISingletonDependency
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100000;
    private static readonly HashAlgorithmName Algorithm = HashAlgorithmName.SHA256;

    public string HashPassword(string password)
    {
        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, HashSize);

        byte[] combined = new byte[SaltSize + HashSize];
        Buffer.BlockCopy(salt, 0, combined, 0, SaltSize);
        Buffer.BlockCopy(hash, 0, combined, SaltSize, HashSize);

        return Convert.ToBase64String(combined);
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        byte[] combined = Convert.FromBase64String(passwordHash);
        if (combined.Length != SaltSize + HashSize)
        {
            return false;
        }

        byte[] salt = new byte[SaltSize];
        byte[] originalHash = new byte[HashSize];
        Buffer.BlockCopy(combined, 0, salt, 0, SaltSize);
        Buffer.BlockCopy(combined, SaltSize, originalHash, 0, HashSize);

        byte[] incomingHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, Algorithm, HashSize);

        return CryptographicOperations.FixedTimeEquals(originalHash, incomingHash);
    }
}
