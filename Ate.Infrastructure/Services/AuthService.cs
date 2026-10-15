using Ate.Application.Common.Interfaces;
using Ate.Application.Features.Auth.DTOs;
using Ate.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Ate.Infrastructure.Services;

public class AuthService : IAuthService, IScopedDependency
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IDateTimeProvider _dateTimeProvider;

    public AuthService(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<LoginResponse?> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.DocumentNumber) || string.IsNullOrWhiteSpace(request.Password))
        {
            return null;
        }

        var normalizedDocumentNumber = request.DocumentNumber.Trim();

        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.Person)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => !u.IsDeleted && u.Person != null && u.Person.DocumentNumber == normalizedDocumentNumber, cancellationToken);

        if (user == null || !user.IsActive || string.IsNullOrEmpty(user.PasswordHash))
        {
            return null;
        }

        var isValidPassword = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);
        if (!isValidPassword)
        {
            return null;
        }

        var roles = user.UserRoles
            .Where(ur => ur.Role != null && !string.IsNullOrWhiteSpace(ur.Role.Name))
            .Select(ur => ur.Role.Name)
            .Distinct()
            .ToList();

        var (accessToken, expiresAt) = _jwtTokenGenerator.GenerateToken(user, roles);
        var refreshTokenString = _jwtTokenGenerator.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            Token = refreshTokenString,
            ExpiresAt = _dateTimeProvider.UtcNow.AddDays(7),
            CreatedAt = _dateTimeProvider.UtcNow,
            IsRevoked = false
        };

        _context.RefreshTokens.Add(refreshToken);

        var trackedUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == user.Id, cancellationToken);
        if (trackedUser != null)
        {
            trackedUser.LastLoginAt = _dateTimeProvider.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);

        var fullName = user.Person != null
            ? $"{user.Person.FirstName} {user.Person.LastName}".Trim()
            : user.Email ?? string.Empty;

        var userDto = new UserDto(
            user.Id,
            user.Email ?? string.Empty,
            fullName,
            user.Person?.AvatarUrl,
            roles);

        return new LoginResponse(accessToken, refreshTokenString, expiresAt, userDto);
    }
}
