using System.Text.RegularExpressions;

namespace Ate.Application.Common.Validation;

/// <summary>
/// Nombres y apellidos de persona: letras (con tildes y ñ), espacios, apóstrofo, guion y punto.
/// Impide que un nombre lleve HTML o enlaces a los correos y PDFs oficiales (OWASP A03).
/// </summary>
public static partial class PersonNameRules
{
    public const string Message = "Usa solo letras, espacios, apóstrofo o guion.";

    [GeneratedRegex(@"^[\p{L}\p{M}](?:[\p{L}\p{M}' .\-]*[\p{L}\p{M}.])?$")]
    private static partial Regex Pattern();

    public static bool IsValid(string? name) =>
        !string.IsNullOrWhiteSpace(name) && Pattern().IsMatch(name.Trim());
}
