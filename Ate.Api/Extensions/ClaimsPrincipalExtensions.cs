using System.Security.Claims;
using Ate.Domain.Constants;

namespace Ate.Api.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static Guid? GetUserId(this ClaimsPrincipal? principal)
    {
        if (principal == null)
        {
            return null;
        }

        var idString = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub");

        return Guid.TryParse(idString, out var userId) ? userId : null;
    }

    public static string GetEmail(this ClaimsPrincipal? principal) =>
        principal?.FindFirstValue(ClaimTypes.Email)
        ?? principal?.FindFirstValue("email")
        ?? string.Empty;

    public static string GetFullName(this ClaimsPrincipal? principal) =>
        principal?.FindFirstValue(ClaimTypes.Name)
        ?? principal?.FindFirstValue("name")
        ?? string.Empty;

    public static string GetDocumentNumber(this ClaimsPrincipal? principal) =>
        principal?.FindFirstValue(CustomClaimTypes.DocumentNumber) ?? string.Empty;

    public static Guid? GetPersonId(this ClaimsPrincipal? principal)
    {
        var personIdString = principal?.FindFirstValue(CustomClaimTypes.PersonId);
        return Guid.TryParse(personIdString, out var personId) ? personId : null;
    }

    public static IReadOnlyList<string> GetRoles(this ClaimsPrincipal? principal)
    {
        if (principal == null)
        {
            return Array.Empty<string>();
        }

        return principal.FindAll(ClaimTypes.Role)
            .Concat(principal.FindAll("role"))
            .Select(c => c.Value)
            .Distinct()
            .ToList();
    }

    public static bool IsInRole(this ClaimsPrincipal? principal, string role) =>
        principal?.IsInRole(role) ?? false;

    public static bool IsSuperAdmin(this ClaimsPrincipal? principal) =>
        principal?.IsInRole(RoleConstants.SuperAdmin) ?? false;

    public static bool IsCompanyAdmin(this ClaimsPrincipal? principal) =>
        principal?.IsInRole(RoleConstants.CompanyAdmin) ?? false;

    public static bool IsEmployee(this ClaimsPrincipal? principal) =>
        principal?.IsInRole(RoleConstants.Employee) ?? false;

    public static bool IsMerchant(this ClaimsPrincipal? principal) =>
        principal?.IsInRole(RoleConstants.Merchant) ?? false;
}
