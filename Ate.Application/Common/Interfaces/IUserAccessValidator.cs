using Ate.Domain.Common;

namespace Ate.Application.Common.Interfaces;

/// <summary>
/// Checks that an account can still use the platform: the user is active and, depending on
/// the role, the employee record and its company are active too. Used at login and on every
/// authenticated request, so deactivating an account revokes access without waiting for the
/// token to expire.
/// </summary>
public interface IUserAccessValidator
{
    /// <returns>null when the account may continue; otherwise the reason it was blocked.</returns>
    Task<Error?> ValidateAsync(Guid userId, CancellationToken cancellationToken = default);
}
