using System.Globalization;
using System.Text;
using Ate.Application.Common.Interfaces;
using Ate.Application.Common.Models.Emails;
using Ate.Application.Features.Restaurants.DTOs;
using Ate.Domain.Common;
using Ate.Domain.Constants;
using Ate.Domain.Entities;
using Ate.Domain.Enums;
using Ate.Domain.Errors;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ate.Infrastructure.Services;

public class AdminRestaurantService : IAdminRestaurantService, IScopedDependency
{
    private const int MaxCodeBaseLength = 40;

    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IValidator<CreateRestaurantRequest> _createValidator;
    private readonly IEmailService _emailService;
    private readonly SmtpOptions _smtpOptions;
    private readonly ILogger<AdminRestaurantService> _logger;

    public AdminRestaurantService(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher,
        IValidator<CreateRestaurantRequest> createValidator,
        IEmailService emailService,
        IOptions<SmtpOptions> smtpOptions,
        ILogger<AdminRestaurantService> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _createValidator = createValidator;
        _emailService = emailService;
        _smtpOptions = smtpOptions.Value;
        _logger = logger;
    }

    public async Task<Result<IReadOnlyList<AdminRestaurantDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var agreementCounts = await _context.CompanyRestaurantAgreements
            .AsNoTracking()
            .Where(a => a.IsActive)
            .GroupBy(a => a.RestaurantId)
            .Select(g => new { RestaurantId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.RestaurantId, x => x.Count, cancellationToken);

        var restaurants = await _context.Restaurants
            .AsNoTracking()
            .OrderBy(r => r.Name)
            .Select(r => new { r.Id, r.Code, r.TaxId, r.Name, r.Cuisine, r.Address, r.LogoUrl, r.Latitude, r.Longitude, r.IsActive, r.CreatedAt })
            .ToListAsync(cancellationToken);

        IReadOnlyList<AdminRestaurantDto> result = restaurants
            .Select(r => new AdminRestaurantDto(
                r.Id, r.Code, r.TaxId, r.Name, r.Cuisine, r.Address, r.LogoUrl, r.Latitude, r.Longitude, r.IsActive,
                agreementCounts.GetValueOrDefault(r.Id), r.CreatedAt))
            .ToList();

        return Result.Success(result);
    }

    public async Task<Result<CreateRestaurantResponse>> CreateAsync(CreateRestaurantRequest request, CancellationToken cancellationToken = default)
    {
        var validationResult = await _createValidator.ValidateAsync(request, cancellationToken);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return Result.Failure<CreateRestaurantResponse>(Error.Validation("Restaurant.InvalidData", "Revisa los datos del restaurante.", errors));
        }

        var taxId = request.TaxId.Trim();
        var merchantDocument = request.MerchantDocumentNumber.Trim();
        var merchantEmail = request.MerchantEmail.Trim().ToLowerInvariant();

        if (await _context.Restaurants.AnyAsync(r => r.TaxId == taxId, cancellationToken))
        {
            return Result.Failure<CreateRestaurantResponse>(AdminRestaurantErrors.TaxIdAlreadyExists);
        }

        if (await _context.Persons.AnyAsync(p => p.DocumentType == DocumentType.NationalId && p.DocumentNumber == merchantDocument, cancellationToken))
        {
            return Result.Failure<CreateRestaurantResponse>(AdminRestaurantErrors.MerchantDocumentAlreadyExists);
        }

        if (await _context.Users.AnyAsync(u => u.Email == merchantEmail, cancellationToken))
        {
            return Result.Failure<CreateRestaurantResponse>(AdminRestaurantErrors.MerchantEmailAlreadyExists);
        }

        var merchantRoleId = await _context.Roles
            .Where(r => r.Name == RoleConstants.Merchant)
            .Select(r => r.Id)
            .FirstAsync(cancellationToken);

        var name = request.Name.Trim();
        var code = await GenerateUniqueCodeAsync(name, cancellationToken);
        var temporaryPassword = TemporaryPasswordGenerator.Generate();

        var user = new User
        {
            Email = merchantEmail,
            PasswordHash = _passwordHasher.HashPassword(temporaryPassword),
            IsActive = true,
            EmailConfirmed = false,
            MustChangePassword = true
        };

        var person = new Person
        {
            UserId = user.Id,
            User = user,
            FirstName = request.MerchantFirstName.Trim(),
            LastName = request.MerchantLastName.Trim(),
            DocumentType = DocumentType.NationalId,
            DocumentNumber = merchantDocument
        };

        var restaurant = new Restaurant
        {
            MerchantUserId = user.Id,
            Code = code,
            TaxId = taxId,
            Name = name,
            Cuisine = request.Cuisine.Trim(),
            Phone = NullIfBlank(request.Phone),
            Address = NullIfBlank(request.Address),
            BrandColor = request.BrandColor.Trim().ToUpperInvariant(),
            ReadyMinutes = request.ReadyMinutes,
            Latitude = request.Latitude,
            Longitude = request.Longitude,
            LogoUrl = string.IsNullOrEmpty(request.Logo) ? null : request.Logo,
            IsActive = true
        };

        _context.Users.Add(user);
        _context.Persons.Add(person);
        _context.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = merchantRoleId,
            AssignedAt = DateTimeOffset.UtcNow
        });
        _context.Restaurants.Add(restaurant);

        await _context.SaveChangesAsync(cancellationToken);

        var merchantFullName = $"{person.FirstName} {person.LastName}".Trim();

        var welcomeEmailSent = false;
        try
        {
            var loginUrl = $"{_smtpOptions.ClientBaseUrl.TrimEnd('/')}/login";
            await _emailService.SendTemplateEmailAsync(
                merchantEmail,
                $"¡Bienvenido a Ate, {restaurant.Name}! Ya eres restaurante aliado",
                EmailTemplateType.WelcomeMerchant,
                new WelcomeMerchantEmailModel(
                    merchantFullName,
                    restaurant.Name,
                    merchantDocument,
                    merchantEmail,
                    temporaryPassword,
                    loginUrl),
                cancellationToken: cancellationToken);
            welcomeEmailSent = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send welcome email to merchant {Email}", merchantEmail);
        }

        return Result.Success(new CreateRestaurantResponse(
            ToDto(restaurant, 0),
            merchantFullName,
            merchantEmail,
            welcomeEmailSent));
    }

    public async Task<Result<AdminRestaurantDto>> UpdateStatusAsync(Guid restaurantId, UpdateRestaurantStatusRequest request, CancellationToken cancellationToken = default)
    {
        var restaurant = await _context.Restaurants.FirstOrDefaultAsync(r => r.Id == restaurantId, cancellationToken);
        if (restaurant == null)
        {
            return Result.Failure<AdminRestaurantDto>(AdminRestaurantErrors.NotFound);
        }

        restaurant.IsActive = request.IsActive;
        await _context.SaveChangesAsync(cancellationToken);

        var agreements = await _context.CompanyRestaurantAgreements
            .CountAsync(a => a.RestaurantId == restaurantId && a.IsActive, cancellationToken);

        return Result.Success(ToDto(restaurant, agreements));
    }

    /// <summary>
    /// Código legible derivado del nombre ("La Sazón Manabita" → "la-sazon-manabita"),
    /// con sufijo numérico si ya está tomado por otro restaurante.
    /// </summary>
    private async Task<string> GenerateUniqueCodeAsync(string name, CancellationToken cancellationToken)
    {
        var baseCode = Slugify(name);
        if (baseCode.Length == 0)
        {
            baseCode = "restaurante";
        }

        var taken = await _context.Restaurants
            .AsNoTracking()
            .Where(r => r.Code == baseCode || r.Code.StartsWith(baseCode + "-"))
            .Select(r => r.Code)
            .ToListAsync(cancellationToken);

        if (!taken.Contains(baseCode))
        {
            return baseCode;
        }

        var suffix = 2;
        while (taken.Contains($"{baseCode}-{suffix}"))
        {
            suffix++;
        }

        return $"{baseCode}-{suffix}";
    }

    private static string Slugify(string value)
    {
        var normalized = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(normalized.Length);
        var pendingDash = false;

        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsAsciiLetterOrDigit(c))
            {
                if (pendingDash && builder.Length > 0)
                {
                    builder.Append('-');
                }

                builder.Append(char.ToLowerInvariant(c));
                pendingDash = false;
            }
            else
            {
                pendingDash = true;
            }

            if (builder.Length >= MaxCodeBaseLength)
            {
                break;
            }
        }

        return builder.ToString().TrimEnd('-');
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static AdminRestaurantDto ToDto(Restaurant restaurant, int activeAgreements) =>
        new(
            restaurant.Id,
            restaurant.Code,
            restaurant.TaxId,
            restaurant.Name,
            restaurant.Cuisine,
            restaurant.Address,
            restaurant.LogoUrl,
            restaurant.Latitude,
            restaurant.Longitude,
            restaurant.IsActive,
            activeAgreements,
            restaurant.CreatedAt);
}
