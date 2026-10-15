using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Ate.Application.Common.Constants;
using Ate.Application.Common.Interfaces;
using Ate.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Ate.Infrastructure.Services;

public class JwtTokenGenerator : IJwtTokenGenerator, ISingletonDependency
{
    private readonly IConfiguration _configuration;
    private readonly IDateTimeProvider _dateTimeProvider;

    public JwtTokenGenerator(IConfiguration configuration, IDateTimeProvider dateTimeProvider)
    {
        _configuration = configuration;
        _dateTimeProvider = dateTimeProvider;
    }

    public (string Token, DateTimeOffset ExpiresAt) GenerateToken(User user, IReadOnlyList<string> roles)
    {
        var section = _configuration.GetSection(ConfigurationConstants.JwtSettings.SectionName);
        var secretKey = section[ConfigurationConstants.JwtSettings.SecretKey]
            ?? throw new InvalidOperationException("JWT SecretKey is not configured.");
        var issuer = section[ConfigurationConstants.JwtSettings.Issuer] ?? "Ate.Api";
        var audience = section[ConfigurationConstants.JwtSettings.Audience] ?? "Ate.Client";
        var expirationMinutes = int.TryParse(section[ConfigurationConstants.JwtSettings.ExpirationInMinutes], out var minutes) ? minutes : 480;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var expiresAt = _dateTimeProvider.UtcNow.AddMinutes(expirationMinutes);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        if (user.Person != null)
        {
            var fullName = $"{user.Person.FirstName} {user.Person.LastName}".Trim();
            if (!string.IsNullOrWhiteSpace(fullName))
            {
                claims.Add(new Claim(ClaimTypes.Name, fullName));
            }
        }

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt.UtcDateTime,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = credentials
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var securityToken = tokenHandler.CreateToken(tokenDescriptor);
        var tokenString = tokenHandler.WriteToken(securityToken);

        return (tokenString, expiresAt);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }
}
