using Ate.Domain.Entities;

namespace Ate.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    (string Token, DateTimeOffset ExpiresAt) GenerateToken(User user, IReadOnlyList<string> roles);
    string GenerateRefreshToken();
}
