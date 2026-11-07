using System.Security.Cryptography;
using System.Text;
using Ate.Application.Common.Interfaces;
using Ate.Application.Common.Models.Emails;
using Ate.Application.Features.PasswordRecovery;
using Ate.Application.Features.PasswordRecovery.DTOs;
using Ate.Domain.Common;
using Ate.Domain.Entities;
using Ate.Domain.Enums;
using Ate.Domain.Errors;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ate.Infrastructure.Services;

/// <summary>
/// Password recovery by email OTP:
/// 1. <see cref="RequestCodeAsync"/> queues an email with a 5-digit code valid for <see cref="PasswordRecoveryOptions.CodeLifetimeMinutes"/>.
/// 2. <see cref="VerifyCodeAsync"/> checks it (expiration + attempts) and issues a single-use reset token with its own expiration.
/// 3. <see cref="ResetPasswordAsync"/> changes the password with that token and revokes every session.
/// Codes and tokens are stored only as keyed hashes; attempts and single use are protected by concurrency tokens.
/// </summary>
public class PasswordRecoveryService : IPasswordRecoveryService, IScopedDependency
{
    private const string EmailSubject = "Tu código para restablecer la contraseña";
    private const int ResetTokenBytes = 32;

    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IEmailQueue _emailQueue;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly PasswordRecoveryOptions _options;
    private readonly byte[] _hashKey;
    private readonly IValidator<RequestPasswordRecoveryRequest> _requestValidator;
    private readonly IValidator<VerifyPasswordRecoveryCodeRequest> _verifyValidator;
    private readonly IValidator<ResetPasswordRequest> _resetValidator;
    private readonly ILogger<PasswordRecoveryService> _logger;

    public PasswordRecoveryService(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IEmailQueue emailQueue,
        IDateTimeProvider dateTimeProvider,
        IOptions<PasswordRecoveryOptions> options,
        IOptions<JwtOptions> jwtOptions,
        IValidator<RequestPasswordRecoveryRequest> requestValidator,
        IValidator<VerifyPasswordRecoveryCodeRequest> verifyValidator,
        IValidator<ResetPasswordRequest> resetValidator,
        ILogger<PasswordRecoveryService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _emailQueue = emailQueue;
        _dateTimeProvider = dateTimeProvider;
        _options = options.Value;
        // Server-side key: a leaked table alone is not enough to brute-force the 5-digit codes offline.
        _hashKey = SHA256.HashData(Encoding.UTF8.GetBytes($"ate:password-recovery:{jwtOptions.Value.SecretKey}"));
        _requestValidator = requestValidator;
        _verifyValidator = verifyValidator;
        _resetValidator = resetValidator;
        _logger = logger;
    }

    public async Task<Result> RequestCodeAsync(RequestPasswordRecoveryRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _requestValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result.Failure(ToValidationError(validation));
        }

        var user = await FindActiveUserAsync(request.Email, cancellationToken);
        if (user == null)
        {
            // Same answer as for an existing account: the endpoint must not reveal which emails are registered.
            return Result.Success();
        }

        var now = _dateTimeProvider.UtcNow;
        var lastHour = await _context.PasswordRecoveries
            .Where(r => r.UserId == user.Id && r.CreatedAt > now.AddHours(-1))
            .Select(r => r.CreatedAt)
            .ToListAsync(cancellationToken);

        var inCooldown = lastHour.Any(createdAt => createdAt > now.AddSeconds(-_options.ResendCooldownSeconds));
        if (inCooldown || lastHour.Count >= _options.MaxCodesPerHour)
        {
            _logger.LogInformation("Password recovery code throttled for user {UserId}.", user.Id);
            return Result.Success();
        }

        await InvalidateOpenRecoveriesAsync(user.Id, now, cancellationToken);

        var code = GenerateCode();
        _context.PasswordRecoveries.Add(new PasswordRecovery
        {
            UserId = user.Id,
            CodeHash = Hash(code),
            CodeExpiresAt = now.AddMinutes(_options.CodeLifetimeMinutes),
            CreatedAt = now
        });
        // A parallel verify touched an older recovery: do not send a code that was not stored.
        if (!await TrySaveAsync(cancellationToken))
        {
            return Result.Success();
        }

        var fullName = user.Person != null ? $"{user.Person.FirstName} {user.Person.LastName}".Trim() : user.Email!;
        var requestedAt = _dateTimeProvider.ConvertToTimeZone(now).ToString("dd/MM/yyyy HH:mm", System.Globalization.CultureInfo.InvariantCulture);
        var model = new PasswordResetCodeEmailModel(
            fullName,
            code,
            code.Select(digit => digit.ToString()).ToList(),
            _options.CodeLifetimeMinutes,
            requestedAt);

        // Queued, not awaited: the response time does not reveal whether the account exists.
        if (!_emailQueue.TryEnqueueTemplate(user.Email!, EmailSubject, EmailTemplateType.PasswordResetCode, model))
        {
            _logger.LogError("Password recovery email for user {UserId} was dropped: email queue is full.", user.Id);
        }

        return Result.Success();
    }

    public async Task<Result<PasswordResetTokenDto>> VerifyCodeAsync(VerifyPasswordRecoveryCodeRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _verifyValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result.Failure<PasswordResetTokenDto>(ToValidationError(validation));
        }

        var user = await FindActiveUserAsync(request.Email, cancellationToken);
        if (user == null)
        {
            return Result.Failure<PasswordResetTokenDto>(PasswordRecoveryErrors.InvalidCode);
        }

        var recovery = await _context.PasswordRecoveries
            .Where(r => r.UserId == user.Id && r.UsedAt == null && r.InvalidatedAt == null && r.CodeVerifiedAt == null)
            .OrderByDescending(r => r.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (recovery == null)
        {
            return Result.Failure<PasswordResetTokenDto>(PasswordRecoveryErrors.InvalidCode);
        }

        var now = _dateTimeProvider.UtcNow;

        // Time token: the code is only accepted inside its validity window.
        if (recovery.IsCodeExpired(now))
        {
            recovery.InvalidatedAt = now;
            await TrySaveAsync(cancellationToken);
            return Result.Failure<PasswordResetTokenDto>(PasswordRecoveryErrors.CodeExpired);
        }

        recovery.Attempts++;

        if (!FixedTimeEquals(Hash(request.Code.Trim()), recovery.CodeHash))
        {
            var exhausted = recovery.Attempts >= _options.MaxAttempts;
            if (exhausted)
            {
                recovery.InvalidatedAt = now;
            }

            if (!await TrySaveAsync(cancellationToken))
            {
                return Result.Failure<PasswordResetTokenDto>(PasswordRecoveryErrors.InvalidCode);
            }

            return Result.Failure<PasswordResetTokenDto>(exhausted ? PasswordRecoveryErrors.TooManyAttempts : PasswordRecoveryErrors.InvalidCode);
        }

        var resetToken = Base64UrlEncode(RandomNumberGenerator.GetBytes(ResetTokenBytes));
        recovery.CodeVerifiedAt = now;
        recovery.ResetTokenHash = Hash(resetToken);
        recovery.ResetTokenExpiresAt = now.AddMinutes(_options.ResetTokenLifetimeMinutes);

        // Another request used this attempt in parallel: do not issue a token for it.
        if (!await TrySaveAsync(cancellationToken))
        {
            return Result.Failure<PasswordResetTokenDto>(PasswordRecoveryErrors.InvalidCode);
        }

        return Result.Success(new PasswordResetTokenDto(resetToken, recovery.ResetTokenExpiresAt.Value));
    }

    public async Task<Result> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default)
    {
        var validation = await _resetValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return Result.Failure(ToValidationError(validation));
        }

        var tokenHash = Hash(request.ResetToken.Trim());
        var recovery = await _context.PasswordRecoveries
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.ResetTokenHash == tokenHash, cancellationToken);

        if (recovery == null || recovery.IsClosed || recovery.User.IsDeleted || !recovery.User.IsActive)
        {
            return Result.Failure(PasswordRecoveryErrors.InvalidResetToken);
        }

        var now = _dateTimeProvider.UtcNow;

        // Time token: the reset token is single-use and only valid inside its window.
        if (recovery.IsResetTokenExpired(now))
        {
            recovery.InvalidatedAt = now;
            await TrySaveAsync(cancellationToken);
            return Result.Failure(PasswordRecoveryErrors.ResetTokenExpired);
        }

        var user = recovery.User;
        user.PasswordHash = _passwordHasher.HashPassword(request.NewPassword);
        user.MustChangePassword = false;
        recovery.UsedAt = now;

        await InvalidateOpenRecoveriesAsync(user.Id, now, cancellationToken, exceptId: recovery.Id);

        // A changed password signs out every existing session.
        var activeTokens = await _context.RefreshTokens
            .Where(t => t.UserId == user.Id && !t.IsRevoked)
            .ToListAsync(cancellationToken);
        foreach (var token in activeTokens)
        {
            token.IsRevoked = true;
        }

        // The same token submitted twice in parallel: only one of them changes the password.
        if (!await TrySaveAsync(cancellationToken))
        {
            return Result.Failure(PasswordRecoveryErrors.InvalidResetToken);
        }

        return Result.Success();
    }

    /// <summary>False when a concurrency token changed underneath (parallel attempt or reuse).</summary>
    private async Task<bool> TrySaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateConcurrencyException)
        {
            return false;
        }
    }

    private Task<User?> FindActiveUserAsync(string email, CancellationToken cancellationToken)
    {
        var normalized = email.Trim().ToLowerInvariant();
        return _context.Users
            .Include(u => u.Person)
            .FirstOrDefaultAsync(u => !u.IsDeleted && u.IsActive && u.Email != null && u.Email.ToLower() == normalized, cancellationToken);
    }

    private async Task InvalidateOpenRecoveriesAsync(Guid userId, DateTimeOffset now, CancellationToken cancellationToken, Guid? exceptId = null)
    {
        var open = await _context.PasswordRecoveries
            .Where(r => r.UserId == userId && r.UsedAt == null && r.InvalidatedAt == null && r.Id != exceptId)
            .ToListAsync(cancellationToken);
        foreach (var recovery in open)
        {
            recovery.InvalidatedAt = now;
        }
    }

    private static string GenerateCode()
    {
        var max = (int)Math.Pow(10, PasswordRecoveryConstants.CodeLength);
        return RandomNumberGenerator.GetInt32(0, max).ToString($"D{PasswordRecoveryConstants.CodeLength}");
    }

    private string Hash(string secret) =>
        Convert.ToHexString(HMACSHA256.HashData(_hashKey, Encoding.UTF8.GetBytes(secret)));

    private static bool FixedTimeEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(a), Encoding.UTF8.GetBytes(b));

    private static string Base64UrlEncode(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

    private static Error ToValidationError(FluentValidation.Results.ValidationResult validation)
    {
        var errors = validation.Errors.Select(e => e.ErrorMessage).Distinct().ToList();
        return Error.Validation("PasswordRecovery.Validation", errors.FirstOrDefault() ?? "Revisa los datos ingresados.", errors);
    }
}
