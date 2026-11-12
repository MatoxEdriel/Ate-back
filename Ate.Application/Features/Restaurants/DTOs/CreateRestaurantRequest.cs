using Ate.Application.Features.Restaurants;

namespace Ate.Application.Features.Restaurants.DTOs;

/// <summary>
/// Alta de un restaurante aliado junto con la cuenta del comercio que lo administra.
/// El comercio completa después su menú y horas límite desde su panel.
/// </summary>
/// <param name="Logo">
/// Opcional. TEMPORAL: logo serializado como data URL base64 (png/webp/jpeg) hasta tener el
/// servidor de imágenes; ver <see cref="RestaurantLogo"/>. El comercio puede cambiarlo luego.
/// </param>
public record CreateRestaurantRequest(
    string TaxId,
    string Name,
    string Cuisine,
    string? Phone,
    string? Address,
    string BrandColor,
    int ReadyMinutes,
    string MerchantDocumentNumber,
    string MerchantFirstName,
    string MerchantLastName,
    string MerchantEmail,
    decimal? Latitude = null,
    decimal? Longitude = null,
    string? Logo = null);

/// <param name="WelcomeEmailSent">
/// Whether the welcome email with the temporary password was sent. The password is never returned.
/// </param>
public record CreateRestaurantResponse(
    AdminRestaurantDto Restaurant,
    string MerchantName,
    string MerchantEmail,
    bool WelcomeEmailSent);
