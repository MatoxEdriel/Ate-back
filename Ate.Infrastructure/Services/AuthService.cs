using Ate.Application.Common.Storage;
using Microsoft.Extensions.Configuration;
using Ate.Application.Common.Security;
using Ate.Application.Common.Interfaces;
using Ate.Application.Features.Auth.DTOs;
using Ate.Domain.Common;
using Ate.Domain.Constants;
using Ate.Domain.Entities;
using Ate.Domain.Enums;
using Ate.Domain.Errors;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Ate.Infrastructure.Services;

public class AuthService : IAuthService, IScopedDependency
{
    private readonly IApplicationDbContext _context;
    private readonly ISecurityEventLog _securityEvents;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<ChangePasswordRequest> _changePasswordValidator;
    private readonly ILoginAttemptLimiter _loginAttemptLimiter;
    private readonly IUserAccessValidator _accessValidator;
    private readonly bool _rehashPasswords;

    private readonly ILogoFiles _logoFiles;

    public AuthService(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IDateTimeProvider dateTimeProvider,
        IValidator<LoginRequest> loginValidator,
        IValidator<ChangePasswordRequest> changePasswordValidator,
        ILoginAttemptLimiter loginAttemptLimiter,
        IUserAccessValidator accessValidator,
        ISecurityEventLog securityEvents,
        IConfiguration configuration,
        ILogoFiles logoFiles)
    {
        _logoFiles = logoFiles;
        _rehashPasswords = configuration.GetValue("Security:RehashPasswords", true);
        _securityEvents = securityEvents;
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _dateTimeProvider = dateTimeProvider;
        _loginValidator = loginValidator;
        _changePasswordValidator = changePasswordValidator;
        _loginAttemptLimiter = loginAttemptLimiter;
        _accessValidator = accessValidator;
    }

    private static string? _dummyPasswordHash;

    /// <summary>Hash with the same cost as a real one, verified when no account matches.</summary>
    private string GetDummyPasswordHash() =>
        _dummyPasswordHash ??= _passwordHasher.HashPassword(Guid.NewGuid().ToString("N"));

    public async Task<Result<AuthSession>> LoginAsync(LoginRequest request, string? clientIp = null, CancellationToken cancellationToken = default)
    {
        var validationResult = await _loginValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            return Result.Failure<AuthSession>(AuthErrors.InvalidCredentials);
        }

        var normalizedDocumentNumber = request.DocumentNumber.Trim();

        if (await _loginAttemptLimiter.GetRetryAfterAsync(normalizedDocumentNumber, clientIp, cancellationToken) is { } retryAfter)
        {
            await _securityEvents.RecordAsync(SecurityEventTypes.LoginBlocked, null, SecurityEventOutcomes.Failure, cancellationToken: cancellationToken);
            return Result.Failure<AuthSession>(AuthErrors.TooManyLoginAttempts(retryAfter));
        }

        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.Person)
            .Include(u => u.Company)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u =>
                !u.IsDeleted &&
                u.Person != null &&
                u.Person.DocumentType == DocumentType.NationalId &&
                u.Person.DocumentNumber == normalizedDocumentNumber,
                cancellationToken);

        var hasUsableHash = !string.IsNullOrEmpty(user?.PasswordHash);
        var isValidPassword = _passwordHasher.VerifyPassword(
            request.Password,
            hasUsableHash ? user!.PasswordHash! : GetDummyPasswordHash());

        if (user == null || !hasUsableHash || !isValidPassword)
        {
            await _loginAttemptLimiter.RegisterFailureAsync(normalizedDocumentNumber, clientIp, cancellationToken);
            await _securityEvents.RecordAsync(SecurityEventTypes.Login, user?.Id, SecurityEventOutcomes.Failure, "invalid_credentials", cancellationToken);
            return Result.Failure<AuthSession>(AuthErrors.InvalidCredentials);
        }

        await _loginAttemptLimiter.ResetAsync(normalizedDocumentNumber, clientIp, cancellationToken);

        var loginBlockedReason = !user.IsActive ? AuthErrors.UserInactive
            : user.Company is { IsActive: false } ? AuthErrors.CompanyInactive
            : user.MustChangePassword && user.TemporaryPasswordExpiresAt < _dateTimeProvider.UtcNow ? AuthErrors.TemporaryPasswordExpired
            : (Error?)null;
        if (loginBlockedReason is { } loginError)
        {
            await _securityEvents.RecordAsync(SecurityEventTypes.Login, user.Id, SecurityEventOutcomes.Failure, loginError.Code, cancellationToken);
            return Result.Failure<AuthSession>(loginError);
        }

        // Also covers employees deactivated by HR and employees of a deactivated company.
        var accessError = await _accessValidator.ValidateAsync(user.Id, cancellationToken: cancellationToken);
        if (accessError is { } blocked)
        {
            await _securityEvents.RecordAsync(SecurityEventTypes.Login, user.Id, SecurityEventOutcomes.Failure, blocked.Code, cancellationToken);
            return Result.Failure<AuthSession>(blocked);
        }

        var roles = user.UserRoles
            .Where(ur => ur.Role != null && !string.IsNullOrWhiteSpace(ur.Role.Name))
            .Select(ur => ur.Role.Name)
            .Distinct()
            .ToList();

        var (accessToken, expiresAt) = _jwtTokenGenerator.GenerateToken(user, roles);
        var (refreshToken, refreshTokenString) = CreateRefreshToken(user.Id, roles);
        _context.RefreshTokens.Add(refreshToken);

        var trackedUser = await _context.Users.FirstOrDefaultAsync(u => u.Id == user.Id, cancellationToken);
        if (trackedUser != null)
        {
            trackedUser.LastLoginAt = _dateTimeProvider.UtcNow;

            // Hash con un costo anterior: se recalcula ahora que tenemos la contraseña en claro.
            // En Development queda apagado: la base es compartida y las versiones anteriores del código
            // no entienden el formato nuevo (fallarían con 500 al iniciar sesión).
            if (_rehashPasswords && !string.IsNullOrEmpty(trackedUser.PasswordHash) && _passwordHasher.NeedsRehash(trackedUser.PasswordHash))
            {
                trackedUser.PasswordHash = _passwordHasher.HashPassword(request.Password);
            }
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _securityEvents.RecordAsync(SecurityEventTypes.Login, user.Id, SecurityEventOutcomes.Success, cancellationToken: cancellationToken);

        var fullName = user.Person != null
            ? $"{user.Person.FirstName} {user.Person.LastName}".Trim()
            : user.Email ?? string.Empty;

        var userDto = new UserDto(
            user.Id,
            user.Email ?? string.Empty,
            fullName,
            user.Person?.AvatarUrl,
            roles,
            ToCompanyDto(user.Company),
            user.MustChangePassword);

        return Result.Success(new AuthSession(new LoginResponse(accessToken, expiresAt, userDto), refreshTokenString, refreshToken.ExpiresAt));
    }

    public async Task<Result<AuthSession>> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _changePasswordValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).Distinct().ToList();
            return Result.Failure<AuthSession>(Error.Validation("Auth.ChangePassword", errors.FirstOrDefault() ?? "Revisa los datos ingresados.", errors));
        }

        var user = await _context.Users
            .Include(u => u.Person)
            .Include(u => u.Company)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);

        if (user == null)
        {
            return Result.Failure<AuthSession>(AuthErrors.InvalidToken);
        }

        if (!user.IsActive)
        {
            return Result.Failure<AuthSession>(AuthErrors.UserInactive);
        }

        if (user.Company is { IsActive: false })
        {
            return Result.Failure<AuthSession>(AuthErrors.CompanyInactive);
        }

        // Also covers employees deactivated by HR and employees of a deactivated company.
        var accessError = await _accessValidator.ValidateAsync(user.Id, cancellationToken: cancellationToken);
        if (accessError is { } blocked)
        {
            return Result.Failure<AuthSession>(blocked);
        }

        if (user.MustChangePassword)
        {
            if (!string.IsNullOrEmpty(user.PasswordHash) && _passwordHasher.VerifyPassword(request.NewPassword, user.PasswordHash))
            {
                return Result.Failure<AuthSession>(AuthErrors.PasswordReused);
            }
        }
        else if (string.IsNullOrEmpty(request.CurrentPassword) ||
                 string.IsNullOrEmpty(user.PasswordHash) ||
                 !_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            await _securityEvents.RecordAsync(SecurityEventTypes.PasswordChanged, user.Id, SecurityEventOutcomes.Failure, "invalid_current_password", cancellationToken);
            return Result.Failure<AuthSession>(AuthErrors.InvalidCurrentPassword);
        }

        var now = _dateTimeProvider.UtcNow;
        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.MustChangePassword = false;
        user.TemporaryPasswordExpiresAt = null;
        user.SecurityStamp = Guid.NewGuid().ToString("N");

        var activeTokens = await _context.RefreshTokens
            .Where(t => t.UserId == user.Id && !t.IsRevoked)
            .ToListAsync(cancellationToken);
        foreach (var token in activeTokens)
        {
            Revoke(token, RevocationReasons.Password);
        }

        var roles = user.UserRoles
            .Where(ur => ur.Role != null && !string.IsNullOrWhiteSpace(ur.Role.Name))
            .Select(ur => ur.Role.Name)
            .Distinct()
            .ToList();

        var (accessToken, expiresAt) = _jwtTokenGenerator.GenerateToken(user, roles);
        var (refreshToken, refreshTokenString) = CreateRefreshToken(user.Id, roles);
        _context.RefreshTokens.Add(refreshToken);

        await _context.SaveChangesAsync(cancellationToken);
        await _securityEvents.RecordAsync(SecurityEventTypes.PasswordChanged, user.Id, SecurityEventOutcomes.Success, cancellationToken: cancellationToken);

        var fullName = user.Person != null
            ? $"{user.Person.FirstName} {user.Person.LastName}".Trim()
            : user.Email ?? string.Empty;

        var userDto = new UserDto(
            user.Id,
            user.Email ?? string.Empty,
            fullName,
            user.Person?.AvatarUrl,
            roles,
            ToCompanyDto(user.Company),
            user.MustChangePassword);

        return Result.Success(new AuthSession(new LoginResponse(accessToken, expiresAt, userDto), refreshTokenString, refreshToken.ExpiresAt));
    }

    public async Task<Result<CurrentUserDto>> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Where(u => u.Id == userId && !u.IsDeleted)
            .Select(u => new
            {
                u.Id,
                u.Email,
                FirstName = u.Person != null ? u.Person.FirstName : string.Empty,
                LastName = u.Person != null ? u.Person.LastName : string.Empty,
                DocumentNumber = u.Person != null ? u.Person.DocumentNumber : string.Empty,
                Roles = u.UserRoles.Select(ur => ur.Role.Name).ToList(),
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
        {
            return Result.Failure<CurrentUserDto>(AuthErrors.InvalidToken);
        }

        return Result.Success(new CurrentUserDto(
            user.Id,
            user.Email ?? string.Empty,
            $"{user.FirstName} {user.LastName}".Trim(),
            user.DocumentNumber,
            user.Roles));
    }

    public async Task<Result<AuthSession>> GenerateDevTokenAsync(DevTokenRequest? request = null, CancellationToken cancellationToken = default)
    {
        var targetRole = string.IsNullOrWhiteSpace(request?.Role) ? RoleConstants.SuperAdmin : request.Role.Trim();
        var roles = targetRole.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();
        if (roles.Count == 0)
        {
            roles.Add(RoleConstants.SuperAdmin);
        }

        var normalizedDocumentNumber = string.IsNullOrWhiteSpace(request?.DocumentNumber) ? "0943676155" : request.DocumentNumber.Trim();
        var targetEmail = string.IsNullOrWhiteSpace(request?.Email) ? $"dev.{roles[0].ToLowerInvariant()}@ate.com" : request.Email.Trim();
        var targetFullName = string.IsNullOrWhiteSpace(request?.FullName) ? $"Dev {roles[0]}" : request.FullName.Trim();

        User? user = null;
        try
        {
            user = await _context.Users
                .AsNoTracking()
                .Include(u => u.Person)
                .Include(u => u.Company)
                .Include(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => !u.IsDeleted && (u.Email == targetEmail || (u.Person != null && u.Person.DocumentNumber == normalizedDocumentNumber)), cancellationToken);
        }
        catch
        {
        }

        if (user == null)
        {
            var nameParts = targetFullName.Split(' ', 2);
            var firstName = nameParts[0];
            var lastName = nameParts.Length > 1 ? nameParts[1] : "Developer";

            user = new User
            {
                Id = Guid.CreateVersion7(),
                Email = targetEmail,
                IsActive = true,
                EmailConfirmed = true,
                MustChangePassword = false,
                Person = new Person
                {
                    Id = Guid.CreateVersion7(),
                    FirstName = firstName,
                    LastName = lastName,
                    DocumentNumber = normalizedDocumentNumber
                }
            };
        }

        var (accessToken, expiresAt) = _jwtTokenGenerator.GenerateToken(user, roles);
        var (refreshToken, refreshTokenString) = CreateRefreshToken(user.Id, roles);

        try
        {
            _context.RefreshTokens.Add(refreshToken);
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch
        {
        }

        var fullName = user.Person != null
            ? $"{user.Person.FirstName} {user.Person.LastName}".Trim()
            : targetFullName;

        var userDto = new UserDto(
            user.Id,
            user.Email ?? targetEmail,
            fullName,
            user.Person?.AvatarUrl,
            roles,
            ToCompanyDto(user.Company),
            user.MustChangePassword);

        return Result.Success(new AuthSession(new LoginResponse(accessToken, expiresAt, userDto), refreshTokenString, refreshToken.ExpiresAt));
    }

    public async Task<Result<AuthSession>> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var now = _dateTimeProvider.UtcNow;
        var tokenHash = RefreshTokenHasher.Hash(refreshToken);

        var stored = await _context.RefreshTokens
            .Include(t => t.User).ThenInclude(u => u.Person)
            .Include(t => t.User).ThenInclude(u => u.Company)
            .Include(t => t.User).ThenInclude(u => u.UserRoles).ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(t => t.Token == tokenHash, cancellationToken);

        if (stored == null || stored.ExpiresAt <= now)
        {
            return Result.Failure<AuthSession>(AuthErrors.InvalidToken);
        }

        var replayWithinGrace = false;
        if (stored.IsRevoked)
        {
            var rotated = stored.RevocationReason == RevocationReasons.Rotated && stored.RevokedAt is not null;
            if (!rotated)
            {
                // Revocado por logout, cambio de contraseña, migración o reuso: simplemente ya no vale.
                // No es señal de robo, y revocar todo aquí sacaría también las sesiones nuevas del usuario.
                return Result.Failure<AuthSession>(AuthErrors.InvalidToken);
            }

            if (now - stored.RevokedAt!.Value > RefreshLifetime.RotationGrace)
            {
                // Un token ya rotado que vuelve a aparecer fuera de la gracia: posible robo. Se cierran todas las sesiones.
                await RevokeAllAsync(stored.UserId, RevocationReasons.Reuse, cancellationToken);
                await _context.SaveChangesAsync(cancellationToken);
                await _securityEvents.RecordAsync(SecurityEventTypes.RefreshReuse, stored.UserId, SecurityEventOutcomes.Failure, "all_sessions_revoked", cancellationToken);
                return Result.Failure<AuthSession>(AuthErrors.InvalidToken);
            }

            // Dentro de la gracia (otra pestaña rotó hace segundos): solo se entrega un access token nuevo.
            // No se crea otro refresh token, así un token robado no puede generar sesiones ilimitadas.
            replayWithinGrace = true;
        }

        var user = stored.User;
        if (user.IsDeleted)
        {
            return Result.Failure<AuthSession>(AuthErrors.InvalidToken);
        }

        if (await _accessValidator.ValidateAsync(user.Id, cancellationToken: cancellationToken) is { } blocked)
        {
            return Result.Failure<AuthSession>(blocked);
        }

        var roles = user.UserRoles
            .Where(ur => ur.Role != null && !string.IsNullOrWhiteSpace(ur.Role.Name))
            .Select(ur => ur.Role.Name)
            .Distinct()
            .ToList();

        var (accessToken, expiresAt) = _jwtTokenGenerator.GenerateToken(user, roles);

        RefreshToken? newToken = null;
        string? newTokenString = null;
        if (!replayWithinGrace)
        {
            Revoke(stored, RevocationReasons.Rotated);
            (newToken, newTokenString) = CreateRefreshToken(user.Id, roles);
            _context.RefreshTokens.Add(newToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var fullName = user.Person != null
            ? $"{user.Person.FirstName} {user.Person.LastName}".Trim()
            : user.Email ?? string.Empty;

        var userDto = new UserDto(
            user.Id,
            user.Email ?? string.Empty,
            fullName,
            user.Person?.AvatarUrl,
            roles,
            ToCompanyDto(user.Company),
            user.MustChangePassword);

        return Result.Success(new AuthSession(new LoginResponse(accessToken, expiresAt, userDto), newTokenString, newToken?.ExpiresAt));
    }

    public async Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            return;
        }

        var tokenHash = RefreshTokenHasher.Hash(refreshToken);
        var stored = await _context.RefreshTokens.FirstOrDefaultAsync(t => t.Token == tokenHash, cancellationToken);
        if (stored is { IsRevoked: false })
        {
            Revoke(stored, RevocationReasons.Logout);
            await _context.SaveChangesAsync(cancellationToken);
            await _securityEvents.RecordAsync(SecurityEventTypes.Logout, stored.UserId, SecurityEventOutcomes.Success, cancellationToken: cancellationToken);
        }
    }

    /// <summary>Nuevo refresh token: el valor en claro va a la cookie; en la base solo su SHA-256.</summary>
    private (RefreshToken Entity, string Raw) CreateRefreshToken(Guid userId, IReadOnlyCollection<string> roles)
    {
        var raw = _jwtTokenGenerator.GenerateRefreshToken();
        var now = _dateTimeProvider.UtcNow;
        var entity = new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = userId,
            Token = RefreshTokenHasher.Hash(raw),
            ExpiresAt = now + RefreshLifetime.For(roles),
            CreatedAt = now,
            IsRevoked = false
        };
        return (entity, raw);
    }

    private void Revoke(RefreshToken token, string reason)
    {
        token.IsRevoked = true;
        token.RevokedAt = _dateTimeProvider.UtcNow;
        token.RevocationReason = reason;
    }

    private async Task RevokeAllAsync(Guid userId, string reason, CancellationToken cancellationToken)
    {
        var active = await _context.RefreshTokens
            .Where(t => t.UserId == userId && !t.IsRevoked)
            .ToListAsync(cancellationToken);
        foreach (var token in active)
        {
            Revoke(token, reason);
        }
    }

    private CompanyDto? ToCompanyDto(Company? company)
    {
        return company == null
            ? null
            : new CompanyDto(company.Id, company.BusinessName, _logoFiles.ToPublicUrl(company.LogoUrl));
    }
}
