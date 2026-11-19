namespace Ate.Application.Features.EmployeeOrders.DTOs;

public record RateOrderRequest(Dictionary<string, bool>? Answers, bool Skipped);
