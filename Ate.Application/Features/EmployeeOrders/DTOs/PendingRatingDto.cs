namespace Ate.Application.Features.EmployeeOrders.DTOs;

public record PendingRatingDto(
    string OrderId,
    Guid RestaurantId,
    string RestaurantName,
    string MealType,
    IReadOnlyList<string> DishNames,
    string DeliveryMode,
    IReadOnlyList<RatingQuestionDto> Questions);

public record RatingQuestionDto(string Category, string Text);
