using FluentValidation;

namespace Ate.Application.Common.Validation;

/// <summary>
/// Validates an Ecuadorian RUC (Registro Único de Contribuyentes): 13 digits and a province code
/// 01–24 or 30. The third digit tells the taxpayer type:
/// 0–5 natural person (the first 10 digits must be a valid cédula and the establishment suffix is not 000),
/// 6 public entity (suffix of 4 digits not 0000), 9 private company (suffix of 3 digits not 000).
/// The modulo-11 check digit of companies is not enforced: the SRI issues valid RUCs that do not satisfy it.
/// </summary>
public static class EcuadorianRuc
{
    private const int Length = 13;
    private const int MaxProvinceCode = 24;
    private const int AbroadProvinceCode = 30;
    private const int MaxNaturalPersonThirdDigit = 5;
    private const int PublicEntityThirdDigit = 6;
    private const int PrivateCompanyThirdDigit = 9;

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

        var thirdDigit = value[2] - '0';
        if (thirdDigit <= MaxNaturalPersonThirdDigit)
            return EcuadorianNationalId.IsValid(value[..10]) && value[10..] != "000";

        return thirdDigit switch
        {
            PublicEntityThirdDigit => value[9..] != "0000",
            PrivateCompanyThirdDigit => value[10..] != "000",
            _ => false
        };
    }

    public static IRuleBuilderOptions<T, string> ValidEcuadorianRuc<T>(this IRuleBuilder<T, string> rule) =>
        rule.Must(IsValid);
}
