using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Ate.Infrastructure.Services;

/// <summary>
/// Fuera de Development no deja arrancar la API con una clave JWT corta o de ejemplo:
/// con una clave conocida cualquiera podría firmar tokens de cualquier rol (OWASP A02).
/// </summary>
public sealed class JwtOptionsValidator : IValidateOptions<JwtOptions>
{
    private static readonly string[] PlaceholderWords = ["REEMPLAZAR", "CHANGE", "SECRET", "DEFECTO", "EXAMPLE", "CLAVE"];
    private const int MinKeyBytes = 32;

    private readonly IHostEnvironment _environment;

    public JwtOptionsValidator(IHostEnvironment environment)
    {
        _environment = environment;
    }

    public ValidateOptionsResult Validate(string? name, JwtOptions options)
    {
        if (_environment.IsDevelopment())
        {
            return ValidateOptionsResult.Success;
        }

        var key = options.SecretKey ?? string.Empty;
        var looksLikePlaceholder = PlaceholderWords.Any(word => key.Contains(word, StringComparison.OrdinalIgnoreCase));

        return Encoding.UTF8.GetByteCount(key) < MinKeyBytes || looksLikePlaceholder
            ? ValidateOptionsResult.Fail("JwtSettings:SecretKey must be a random value of at least 32 bytes.")
            : ValidateOptionsResult.Success;
    }
}
