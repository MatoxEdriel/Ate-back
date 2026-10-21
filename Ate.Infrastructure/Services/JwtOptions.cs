using System.ComponentModel.DataAnnotations;

namespace Ate.Infrastructure.Services;

public class JwtOptions
{
    public const string SectionName = "JwtSettings";

    [Required(ErrorMessage = "JWT SecretKey is required.")]
    [MinLength(32, ErrorMessage = "JWT SecretKey must be at least 32 characters long.")]
    public string SecretKey { get; set; } = string.Empty;

    [Required(ErrorMessage = "JWT Issuer is required.")]
    public string Issuer { get; set; } = "Ate.Api";

    [Required(ErrorMessage = "JWT Audience is required.")]
    public string Audience { get; set; } = "Ate.Client";

    [Range(1, 43200, ErrorMessage = "JWT ExpirationInMinutes must be between 1 and 43200.")]
    public int ExpirationInMinutes { get; set; } = 480;
}
