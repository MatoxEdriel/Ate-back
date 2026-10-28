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
