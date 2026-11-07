namespace Ate.Application.Common.Models.Emails;

public record WelcomeUserEmailModel(
    string FullName,
    string Email,
    string TemporaryPassword,
    string LoginUrl);

public record PasswordResetEmailModel(
    string FullName,
    string ResetUrl,
    int ExpirationMinutes);

/// <param name="CodeDigits">The code split in digits, one box per digit in the template.</param>
/// <param name="RequestedAt">When it was requested, in business time (dd/MM/yyyy HH:mm).</param>
public record PasswordResetCodeEmailModel(
    string FullName,
    string Code,
    IReadOnlyList<string> CodeDigits,
    int ExpirationMinutes,
    string RequestedAt);

public record OrderConfirmationEmailModel(
    string OrderNumber,
    string FullName,
    string RestaurantName,
    string MealType,
    decimal TotalAmount,
    string PickupCode);

public record OrderStatusChangedEmailModel(
    string OrderNumber,
    string FullName,
    string RestaurantName,
    string NewStatus,
    string? Notes);

public record MonthlyAllowanceRefillEmailModel(
    string FullName,
    string MonthName,
    decimal AllowanceAmount,
    decimal Balance);
