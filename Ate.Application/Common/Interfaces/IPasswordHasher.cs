namespace Ate.Application.Common.Interfaces;

public interface IPasswordHasher
{
    string HashPassword(string password);
    bool VerifyPassword(string password, string passwordHash);

    /// <summary>True si el hash usa un formato o un costo anterior y debe recalcularse en el próximo login.</summary>
    bool NeedsRehash(string passwordHash);
}
