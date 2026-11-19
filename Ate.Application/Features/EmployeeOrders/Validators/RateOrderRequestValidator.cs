using Ate.Application.Features.EmployeeOrders.DTOs;
using Ate.Domain.Constants;
using FluentValidation;

namespace Ate.Application.Features.EmployeeOrders.Validators;

public class RateOrderRequestValidator : AbstractValidator<RateOrderRequest>
{
    public RateOrderRequestValidator()
    {
        RuleFor(x => x.Answers)
            .Must(answers => answers == null || answers.Keys.All(RatingCategories.All.Contains))
            .WithMessage("Hay una pregunta de calificación que no existe.");

        RuleFor(x => x.Answers)
            .Must(answers => answers is { Count: > 0 })
            .When(x => !x.Skipped)
            .WithMessage("Responde al menos una pregunta o elige omitir.");

        RuleFor(x => x.Answers)
            .Must(answers => answers == null || answers.Count == 0)
            .When(x => x.Skipped)
            .WithMessage("Si omites la calificación no envíes respuestas.");
    }
}
