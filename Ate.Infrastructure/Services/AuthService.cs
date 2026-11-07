using Ate.Application.Common.Interfaces;
using Ate.Application.Features.Auth.DTOs;
using Ate.Domain.Common;
using Ate.Domain.Constants;
using Ate.Domain.Entities;
using Ate.Domain.Errors;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace Ate.Infrastructure.Services;

public class AuthService : IAuthService, IScopedDependency
{
    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<ChangePasswordRequest> _changePasswordValidator;

    public AuthService(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IJwtTokenGenerator jwtTokenGenerator,
        IDateTimeProvider dateTimeProvider,
        IValidator<LoginRequest> loginValidator,
        IValidator<ChangePasswordRequest> changePasswordValidator)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _jwtTokenGenerator = jwtTokenGenerator;
        _dateTimeProvider = dateTimeProvider;
        _loginValidator = loginValidator;
        _changePasswordValidator = changePasswordValidator;
    }

    public async Task<Result<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _loginValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return Result.Failure<LoginResponse>(Error.Validation("Auth.MissingCredentials", "Revisa los datos ingresados.", errors));
        }

        var normalizedDocumentNumber = request.DocumentNumber.Trim();

        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.Person)
            .Include(u => u.Company)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => !u.IsDeleted && u.Person != null && u.Person.DocumentNumber == normalizedDocumentNumber, cancellationToken);

        if (user == null)
        {
            return Result.Failure<LoginResponse>(AuthErrors.InvalidCredentials);
        }

        if (!user.IsActive)
        {
            return Result.Failure<LoginResponse>(AuthErrors.UserInactive);
        }

        if (string.IsNullOrEmpty(user.PasswordHash))
        {
            return Result.Failure<LoginResponse>(AuthErrors.InvalidCredentials);
        }

        var isValidPassword = _passwordHasher.VerifyPassword(request.Password, user.PasswordHash);
        if (!isValidPassword)
        {
            return Result.Failure<LoginResponse>(AuthErrors.InvalidCredentials);
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
            roles,
            ToCompanyDto(user.Company),
            user.MustChangePassword);

        return Result.Success(new LoginResponse(accessToken, refreshTokenString, expiresAt, userDto));
    }

    public async Task<Result<LoginResponse>> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _changePasswordValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).Distinct().ToList();
            return Result.Failure<LoginResponse>(Error.Validation("Auth.ChangePassword", errors.FirstOrDefault() ?? "Revisa los datos ingresados.", errors));
        }

        var user = await _context.Users
            .Include(u => u.Person)
            .Include(u => u.Company)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted, cancellationToken);

        if (user == null)
        {
            return Result.Failure<LoginResponse>(AuthErrors.InvalidToken);
        }

        if (!user.IsActive)
        {
            return Result.Failure<LoginResponse>(AuthErrors.UserInactive);
        }

        if (user.MustChangePassword)
        {
            // Mandatory change: the session was opened with the temporary password moments ago,
            // so it is not asked again; it just cannot be kept.
            if (!string.IsNullOrEmpty(user.PasswordHash) && _passwordHasher.VerifyPassword(request.NewPassword, user.PasswordHash))
            {
                return Result.Failure<LoginResponse>(AuthErrors.PasswordReused);
            }
        }
        else if (string.IsNullOrEmpty(request.CurrentPassword) ||
                 string.IsNullOrEmpty(user.PasswordHash) ||
                 !_passwordHasher.VerifyPassword(request.CurrentPassword, user.PasswordHash))
        {
            return Result.Failure<LoginResponse>(AuthErrors.InvalidCurrentPassword);
        }

        var now = _dateTimeProvider.UtcNow;
        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.MustChangePassword = false;

        // A new password closes every other session (they were opened with the old one).
        var activeTokens = await _context.RefreshTokens
            .Where(t => t.UserId == user.Id && !t.IsRevoked)
            .ToListAsync(cancellationToken);
        foreach (var token in activeTokens)
        {
            token.IsRevoked = true;
        }

        var roles = user.UserRoles
            .Where(ur => ur.Role != null && !string.IsNullOrWhiteSpace(ur.Role.Name))
            .Select(ur => ur.Role.Name)
            .Distinct()
            .ToList();

        // Fresh session: the new access token no longer carries the must_change_password claim.
        var (accessToken, expiresAt) = _jwtTokenGenerator.GenerateToken(user, roles);
        var refreshTokenString = _jwtTokenGenerator.GenerateRefreshToken();
        _context.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.CreateVersion7(),
            UserId = user.Id,
            Token = refreshTokenString,
            ExpiresAt = now.AddDays(7),
            CreatedAt = now,
            IsRevoked = false
        });

        await _context.SaveChangesAsync(cancellationToken);

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

        return Result.Success(new LoginResponse(accessToken, refreshTokenString, expiresAt, userDto));
    }

    public async Task<Result<LoginResponse>> GenerateDevTokenAsync(DevTokenRequest? request = null, CancellationToken cancellationToken = default)
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
        var refreshTokenString = _jwtTokenGenerator.GenerateRefreshToken();

        try
        {
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

        return Result.Success(new LoginResponse(accessToken, refreshTokenString, expiresAt, userDto));
    }

    private static CompanyDto? ToCompanyDto(Company? company)
    {
        return company == null
            ? null
            : new CompanyDto(company.Id, company.BusinessName, company.LogoUrl);
    }
}
