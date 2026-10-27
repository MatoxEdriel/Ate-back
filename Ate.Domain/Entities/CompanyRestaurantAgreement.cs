using Ate.Domain.Common;

namespace Ate.Domain.Entities;

/// <summary>
/// Convenio entre una empresa cliente y un restaurante aliado: define qué restaurantes ven sus colaboradores.
/// </summary>
public class CompanyRestaurantAgreement : BaseAuditableEntity
{
    public Guid CompanyId { get; set; }
    public Company Company { get; set; } = null!;
    public Guid RestaurantId { get; set; }
    public Restaurant Restaurant { get; set; } = null!;
    /// <summary>Distancia aproximada del restaurante a la empresa, en metros.</summary>
    public int DistanceM { get; set; }
    public DateOnly StartsOn { get; set; }
    public DateOnly? EndsOn { get; set; }
    public bool IsActive { get; set; } = true;

    public bool IsValidOn(DateOnly date) =>
        IsActive && StartsOn <= date && (EndsOn == null || EndsOn >= date);
}
