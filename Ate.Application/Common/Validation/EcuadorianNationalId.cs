using FluentValidation;

namespace Ate.Application.Common.Validation;

/// <summary>
/// Validates an Ecuadorian national ID (cédula de identidad) for natural persons:
/// 10 digits, province code 01–24 or 30 (Ecuadorians registered abroad),
/// third digit 0–5, and a modulo-10 check digit (coefficients 2,1,2,1,2,1,2,1,2).
/// </summary>
public static class EcuadorianNationalId
{
    private const int Length = 10;
    private const int MaxProvinceCode = 24;
    private const int AbroadProvinceCode = 30;
    private const int MaxNaturalPersonThirdDigit = 5;

    public static bool IsValid(string? value)
    {
        if (value is null || value.Length != Length)
            return false;

        foreach (var c in value)
        {
            if (c is < '0' or > '9')
                return false;
        }

        var province = (value[0] - '0') * 10 + (value[1] - '0');
        if (province is < 1 or > MaxProvinceCode && province != AbroadProvinceCode)
            return false;

        if (value[2] - '0' > MaxNaturalPersonThirdDigit)
            return false;

        var sum = 0;
        for (var i = 0; i < Length - 1; i++)
        {
            var product = (value[i] - '0') * (i % 2 == 0 ? 2 : 1);
            sum += product > 9 ? product - 9 : product;
        }

        var expectedCheckDigit = (10 - sum % 10) % 10;
        return expectedCheckDigit == value[Length - 1] - '0';
    }

    public static IRuleBuilderOptions<T, string> ValidEcuadorianNationalId<T>(this IRuleBuilder<T, string> rule) =>
        rule.Must(IsValid);
}
