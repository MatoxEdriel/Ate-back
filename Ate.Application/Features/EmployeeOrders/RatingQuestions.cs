using Ate.Application.Features.EmployeeOrders.DTOs;
using Ate.Domain.Constants;

namespace Ate.Application.Features.EmployeeOrders;

public static class RatingQuestions
{
    public static IReadOnlyList<RatingQuestionDto> For(string deliveryMode)
    {
        var delivered = deliveryMode == "entrega";
        var questions = new List<RatingQuestionDto>
        {
            new(RatingCategories.Punctual, delivered
                ? "¿Llegó a la hora esperada?"
                : "¿Estuvo listo en el tiempo que te indicaron?"),
            new(RatingCategories.Accurate, "¿Recibiste exactamente lo que pediste?"),
            new(RatingCategories.Tasty, "¿La comida estaba buena?"),
        };

        if (RatingCategories.AppliesTo(RatingCategories.Packaging, deliveryMode))
        {
            questions.Add(new(RatingCategories.Packaging, delivered
                ? "¿Llegó en buen estado y bien empacado?"
                : "¿Te lo entregaron bien empacado?"));
        }

        return questions;
    }
}
