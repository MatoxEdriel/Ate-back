using System.Text.RegularExpressions;

namespace Ate.Application.Features.Restaurants;

/// <summary>
/// El front lo reduce a 256 px; ~300 mil caracteres (~220 KB) sobran para eso.
/// </summary>
public static class RestaurantLogo
{
    public const int MaxLength = 300_000;

    private static readonly Regex DataUrl = new(
        @"^data:image/(png|webp|jpeg);base64,[A-Za-z0-9+/]+={0,2}$",
        RegexOptions.Compiled);

    public static bool IsValidDataUrl(string? value) => value != null && DataUrl.IsMatch(value);
}
